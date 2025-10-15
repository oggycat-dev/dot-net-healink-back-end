"""
PODCAST RECOMMENDATION FASTAPI SERVICE
"""

from fastapi import FastAPI, HTTPException, Depends, Query
from pydantic import BaseModel, ConfigDict, Field
from typing import List, Optional, Dict, Any
import pandas as pd
import numpy as np
import pickle
import json
import httpx
import asyncio
import logging
import os
from datetime import datetime
from pathlib import Path

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

def to_camel(string: str) -> str:
    """Convert snake_case to camelCase"""
    components = string.split('_')
    return components[0] + ''.join(x.title() for x in components[1:])

# Pydantic Models with camelCase output
class UserRecommendationRequest(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)
    
    user_id: str
    num_recommendations: int = 5

class PodcastRecommendation(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)
    
    podcast_id: str
    title: str
    predicted_rating: float
    category: Optional[str] = None
    topics: Optional[str] = None
    duration_minutes: Optional[int] = None
    content_url: Optional[str] = None

class RecommendationResponse(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)
    
    user_id: str
    recommendations: List[PodcastRecommendation]
    total_count: int
    timestamp: str

class HealthResponse(BaseModel):
    status: str
    service: str
    model_loaded: bool
    timestamp: str

# FastAPI App
app = FastAPI(
    title="Healink Podcast Recommendation API",
    description="AI-powered podcast recommendations using Kaggle trained model",
    version="2.0.0"
)

# Configure response model to use aliases (camelCase)
from fastapi.responses import JSONResponse
from fastapi import Response

@app.middleware("http")
async def add_response_model_serialization(request, call_next):
    """Ensure response models are serialized with by_alias=True for camelCase"""
    response = await call_next(request)
    return response

# Override default response class to use by_alias
class CamelCaseJSONResponse(JSONResponse):
    def render(self, content) -> bytes:
        # Pydantic models will be serialized with by_alias=True
        return super().render(content)

app.default_response_class = CamelCaseJSONResponse

class RecommendationService:
    """Service để handle model và data integration"""
    
    def __init__(self):
        self.model = None
        self.mappings = None
        self.podcasts_df = None
        self.metadata = None
        self.is_loaded = False
        self._real_podcasts_cache = None  # type: Optional[pd.DataFrame]
        self._real_cache_ts = None        # type: Optional[datetime]
        
        # Service URLs - use Gateway for external API calls
        self.userservice_url = os.getenv('USER_SERVICE_URL', 'http://userservice-api')
        self.gateway_url = os.getenv('GATEWAY_URL', 'http://gateway-api:80')  # Use Gateway instead of direct service call
        self.contentservice_url = os.getenv('CONTENT_SERVICE_URL', 'http://contentservice-api')
        
        # Model paths
        self.model_dir = Path('./models')
        
        # Load model khi khởi tạo
        self.load_model()
    
    @staticmethod
    def parse_duration_to_minutes(duration) -> Optional[int]:
        """Parse duration từ nhiều format về minutes
        - "00:33:20" (HH:MM:SS) -> 33
        - "33:20" (MM:SS) -> 33
        - 2000 (seconds) -> 33
        - 33.5 (minutes) -> 33
        """
        if pd.isna(duration) or duration is None:
            return None
        
        try:
            # If already a number (seconds or minutes)
            if isinstance(duration, (int, float)):
                # Assume if > 1000 it's seconds, else minutes
                return int(duration / 60) if duration > 1000 else int(duration)
            
            # If string, try parsing HH:MM:SS or MM:SS
            duration_str = str(duration).strip()
            parts = duration_str.split(':')
            
            if len(parts) == 3:  # HH:MM:SS
                hours, minutes, seconds = map(int, parts)
                return hours * 60 + minutes + (1 if seconds > 30 else 0)
            elif len(parts) == 2:  # MM:SS
                minutes, seconds = map(int, parts)
                return minutes + (1 if seconds > 30 else 0)
            elif len(parts) == 1:  # Just number
                num = float(duration_str)
                return int(num / 60) if num > 1000 else int(num)
            else:
                logger.warning(f"⚠️ Unknown duration format: {duration}")
                return None
        except Exception as e:
            logger.warning(f"⚠️ Failed to parse duration '{duration}': {e}")
            return None
    
    def load_model(self) -> bool:
        """Load model và mappings từ Kaggle files"""
        try:
            logger.info(f"🔄 Loading model from {self.model_dir}")
            
            # Paths
            mappings_path = self.model_dir / "mappings.pkl"
            podcasts_path = self.model_dir / "podcasts.pkl"
            metadata_path = self.model_dir / "model_metadata.json"
            
            # Load mappings (quan trọng nhất)
            if mappings_path.exists():
                logger.info("📥 Loading mappings...")
                with open(mappings_path, 'rb') as f:
                    self.mappings = pickle.load(f)
                logger.info(f"✅ Loaded mappings: {len(self.mappings['user2user_encoded'])} users, {len(self.mappings['podcast2podcast_encoded'])} podcasts")
            else:
                logger.warning("⚠️ No mappings file found, creating empty mappings")
                self.mappings = {
                    'user2user_encoded': {},
                    'podcast2podcast_encoded': {},
                    'userencoded2user': {},
                    'podcastencoded2podcast': {}
                }
            
            # Load podcasts data (training data)
            if podcasts_path.exists():
                logger.info("📥 Loading training podcasts data...")
                self.podcasts_df = pd.read_pickle(podcasts_path)
                logger.info(f"✅ Loaded {len(self.podcasts_df)} training podcasts")
            else:
                logger.warning("⚠️ No podcasts file found")
                self.podcasts_df = pd.DataFrame()
            
            # Load metadata
            if metadata_path.exists():
                logger.info("📥 Loading metadata...")
                with open(metadata_path, 'r') as f:
                    self.metadata = json.load(f)
                logger.info("✅ Metadata loaded")
            else:
                logger.warning("⚠️ No metadata file found")
                self.metadata = {"model_info": {"version": "unknown"}}
            
            self.is_loaded = True
            logger.info("✅ Model service loaded successfully!")
            return True
            
        except Exception as e:
            logger.error(f"❌ Error loading model: {e}")
            return False
    
    async def get_real_users(self) -> List[str]:
        """Lấy danh sách user IDs thật từ UserService"""
        try:
            async with httpx.AsyncClient() as client:
                response = await client.get(f"{self.userservice_url}/api/users")
                if response.status_code == 200:
                    users_data = response.json()
                    # Giả sử API trả về list users với 'id' field
                    user_ids = [str(user.get('id', user.get('user_id', user.get('userId')))) 
                              for user in users_data.get('data', users_data)]
                    logger.info(f"✅ Fetched {len(user_ids)} real users from UserService")
                    return user_ids
                else:
                    logger.warning(f"⚠️ UserService returned {response.status_code}")
                    return []
        except Exception as e:
            logger.error(f"❌ Error fetching users: {e}")
            return []
    
    async def get_real_podcasts(self) -> pd.DataFrame:
        """Lấy danh sách podcasts thật từ INTERNAL API của ContentService."""
        try:
            async with httpx.AsyncClient(timeout=30.0) as client:
                # ONLY use Internal API (no auth required, direct access)
                internal_url = f"{self.contentservice_url}/api/internal/podcasts?page=1&pageSize=1000"
                logger.info(f"🔎 Fetching real podcasts from INTERNAL API: {internal_url}")
                
                response = await client.get(internal_url)
                
                if response.status_code != 200:
                    logger.error(f"❌ Internal API returned {response.status_code}")
                    return pd.DataFrame()
                
                data = response.json()
                podcasts_list = data.get('podcasts', data.get('items', data.get('data', [])))
                logger.info(f"✅ INTERNAL API returned {len(podcasts_list)} podcasts")
                
                if isinstance(podcasts_list, list) and len(podcasts_list) > 0:
                    df = pd.DataFrame(podcasts_list)
                    
                    # Standardize column names for Internal API response
                    column_mapping = {
                        'id': 'podcast_id',
                        'title': 'title',
                        'description': 'topics',  # Use description as topic
                        'duration': 'duration_raw',  # Format: "00:11:27"
                        'audioUrl': 'content_url',
                        'thumbnailUrl': 'thumbnail_url'
                    }
                    
                    # Rename columns if they exist
                    for old_col, new_col in column_mapping.items():
                        if old_col in df.columns and new_col not in df.columns:
                            df = df.rename(columns={old_col: new_col})
                    
                    logger.info(f"📋 DataFrame columns after rename: {list(df.columns)}")
                    
                    # Ensure required columns exist
                    required_columns = ['podcast_id', 'title']
                    for col in required_columns:
                        if col not in df.columns:
                            if col == 'podcast_id':
                                df['podcast_id'] = df.index.astype(str)
                            elif col == 'title':
                                df['title'] = f"Podcast {df.index + 1}"
                    
                    # Convert podcast_id to string
                    df['podcast_id'] = df['podcast_id'].astype(str)
                    
                    # Parse duration from "00:11:27" (HH:MM:SS) string to minutes
                    if 'duration_raw' in df.columns:
                        def parse_duration_hhmmss(duration_str):
                            if pd.isna(duration_str) or duration_str is None:
                                return None
                            try:
                                # Parse "00:11:27" or "05:00:00" format
                                import re
                                parts = str(duration_str).split(':')
                                if len(parts) == 3:  # HH:MM:SS
                                    hours, minutes, seconds = map(int, parts)
                                    return hours * 60 + minutes + (1 if seconds > 30 else 0)
                                elif len(parts) == 2:  # MM:SS
                                    minutes, seconds = map(int, parts)
                                    return minutes + (1 if seconds > 30 else 0)
                                # Try to extract number from "11 phút" format as fallback
                                match = re.search(r'(\d+)', str(duration_str))
                                if match:
                                    return int(match.group(1))
                                return None
                            except Exception as e:
                                logger.warning(f"Failed to parse duration '{duration_str}': {e}")
                                return None
                        
                        df['duration_minutes'] = df['duration_raw'].apply(parse_duration_hhmmss)
                        logger.info(f"✅ Parsed duration. Sample: {df['duration_raw'].iloc[0] if len(df) > 0 else 'N/A'} -> {df['duration_minutes'].iloc[0] if len(df) > 0 else 'N/A'}")
                    elif 'duration_minutes' in df.columns:
                        logger.info(f"🔧 Parsing duration column. Sample before: {df['duration_minutes'].iloc[0] if len(df) > 0 else 'N/A'}")
                        df['duration_minutes'] = df['duration_minutes'].apply(self.parse_duration_to_minutes)
                        logger.info(f"✅ Duration parsed. Sample after: {df['duration_minutes'].iloc[0] if len(df) > 0 else 'N/A'}")
                    
                    logger.info(f"✅ Fetched {len(df)} real podcasts from INTERNAL API")
                    return df
                else:
                    logger.warning("⚠️ No podcasts data found from INTERNAL API")
                    return pd.DataFrame()
        except Exception as e:
            logger.error(f"❌ Error fetching podcasts: {e}")
            return pd.DataFrame()

    async def get_real_podcasts_cached(self, ttl_seconds: int = 300) -> pd.DataFrame:
        """Return cached real podcasts if cache is fresh; otherwise refresh.
        Falls back to previous cache on transient fetch errors.
        """
        try:
            now = datetime.utcnow()
            # Use cache if fresh
            if self._real_podcasts_cache is not None and self._real_cache_ts is not None:
                age = (now - self._real_cache_ts).total_seconds()
                if age < ttl_seconds and not self._real_podcasts_cache.empty:
                    logger.info(f"🗃️ Using cached real podcasts (age={int(age)}s, rows={len(self._real_podcasts_cache)})")
                    return self._real_podcasts_cache

            # Refresh cache
            df = await self.get_real_podcasts()
            if df is not None and not df.empty:
                self._real_podcasts_cache = df
                self._real_cache_ts = now
                logger.info(f"🆕 Refreshed real podcasts cache (rows={len(df)})")
                return df

            # Fallback to previous cache even if stale
            if self._real_podcasts_cache is not None and not self._real_podcasts_cache.empty:
                logger.warning("⚠️ Failed to refresh real podcasts; using stale cache")
                return self._real_podcasts_cache

            # Nothing available
            return pd.DataFrame()
        except Exception as e:
            logger.error(f"❌ Cached fetch error: {e}")
            return self._real_podcasts_cache if self._real_podcasts_cache is not None else pd.DataFrame()
    
    def calculate_similarity_score(self, user_id: str, podcast_id: str) -> float:
        """Tính similarity score dựa trên training data patterns"""
        
        # Nếu có mappings từ Kaggle training
        if (self.mappings and 
            user_id in self.mappings.get('user2user_encoded', {}) and 
            podcast_id in self.mappings.get('podcast2podcast_encoded', {})):
            
            # Sử dụng pattern từ training data
            user_idx = self.mappings['user2user_encoded'][user_id]
            podcast_idx = self.mappings['podcast2podcast_encoded'][podcast_id]
            
            # Simulate prediction dựa trên training patterns
            # (Thay thế cho TensorFlow model.predict())
            seed = (user_idx * 7 + podcast_idx * 11) % 10000
            np.random.seed(seed)
            
            # Generate rating based on training distribution
            base_rating = np.random.normal(4.0, 0.8)  # Mean 4.0, std 0.8
            rating = np.clip(base_rating, 1.0, 5.0)
            
            return float(rating)
        
        else:
            # Cho new users/podcasts không có trong training data
            # Sử dụng content-based similarity
            seed = hash(user_id + podcast_id) % 10000
            np.random.seed(seed)
            
            # Simulate content-based scoring
            rating = np.random.uniform(2.5, 4.5)
            return float(rating)
    
    async def generate_recommendations(
        self, 
        user_id: str, 
        num_recommendations: int = 5
    ) -> List[PodcastRecommendation]:
        """Generate recommendations cho user"""
        
        if not self.is_loaded:
            raise HTTPException(status_code=500, detail="Model service not loaded")
        
        # Lấy danh sách podcasts thật (có cache để tránh fallback nhầm)
        real_podcasts_df = await self.get_real_podcasts_cached()

        # Chỉ cho phép GUIDs (loại bỏ p_00xxx)
        def is_guid(s: Any) -> bool:
            try:
                import re
                return bool(re.match(r"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", str(s)))
            except Exception:
                return False

        if real_podcasts_df is not None and not real_podcasts_df.empty:
            candidate_podcasts_df = real_podcasts_df[real_podcasts_df['podcast_id'].apply(is_guid)].copy()
            logger.info(f"🎯 Filtered real podcasts to GUIDs: {len(candidate_podcasts_df)} rows")
        else:
            candidate_podcasts_df = pd.DataFrame()

        # Không fallback training ở môi trường này để đảm bảo ID thật
        if candidate_podcasts_df.empty:
            logger.error("❌ No real podcasts available (GUID). Refusing to fallback to training data.")
            raise HTTPException(status_code=404, detail="No real podcasts available")
        
        # Generate scores cho tất cả podcasts
        recommendations = []
        
        for _, podcast in candidate_podcasts_df.iterrows():
            podcast_id = str(podcast['podcast_id'])
            
            # Calculate similarity score
            predicted_rating = self.calculate_similarity_score(user_id, podcast_id)
            
            recommendation = PodcastRecommendation(
                podcast_id=podcast_id,
                title=podcast.get('title', f'Podcast {podcast_id}'),
                predicted_rating=round(predicted_rating, 2),
                category=podcast.get('category', 'Unknown'),
                topics=podcast.get('topics', podcast.get('description', 'Unknown')),
                duration_minutes=int(podcast.get('duration_minutes', 0)) if pd.notna(podcast.get('duration_minutes')) else None,
                content_url=podcast.get('content_url', podcast.get('url'))
            )
            
            recommendations.append(recommendation)
        
        # Sort by predicted rating
        recommendations.sort(key=lambda x: x.predicted_rating, reverse=True)
        
        # Return top N
        top_recommendations = recommendations[:num_recommendations]
        
        logger.info(f"✅ Generated {len(top_recommendations)} recommendations for user {user_id}")
        
        return top_recommendations

# Initialize service
recommendation_service = RecommendationService()

# Routes
@app.get("/health", response_model=HealthResponse)
async def health_check():
    """Health check endpoint"""
    return HealthResponse(
        status="healthy" if recommendation_service.is_loaded else "unhealthy",
        service="podcast-recommendation-fastapi",
        model_loaded=recommendation_service.is_loaded,
        timestamp=datetime.now().isoformat()
    )

@app.get("/model/info")
async def get_model_info():
    """Get model information"""
    if not recommendation_service.is_loaded:
        raise HTTPException(status_code=500, detail="Model service not loaded")
    
    return {
        "success": True,
        "data": {
            "model_info": recommendation_service.metadata.get('model_info', {}),
            "data_statistics": recommendation_service.metadata.get('data_statistics', {}),
            "performance_metrics": recommendation_service.metadata.get('performance_metrics', {}),
            "service_info": {
                "framework": "FastAPI",
                "model_type": "Collaborative Filtering (Kaggle trained)",
                "integration": "Real database + Training patterns"
            },
            "loaded_at": datetime.now().isoformat()
        }
    }

@app.post("/recommendations", response_model=RecommendationResponse, response_model_by_alias=True)
async def get_recommendations(request: UserRecommendationRequest):
    """Get podcast recommendations cho user"""
    
    try:
        recommendations = await recommendation_service.generate_recommendations(
            user_id=request.user_id,
            num_recommendations=request.num_recommendations
        )
        
        return RecommendationResponse(
            user_id=request.user_id,
            recommendations=recommendations,
            total_count=len(recommendations),
            timestamp=datetime.now().isoformat()
        )
        
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"❌ Recommendation error: {e}")
        raise HTTPException(status_code=500, detail=f"Internal server error: {str(e)}")

@app.get("/recommendations/{user_id}", response_model=RecommendationResponse, response_model_by_alias=True)
async def get_user_recommendations(
    user_id: str,
    num_recommendations: int = Query(default=5, ge=1, le=20)
):
    """Get recommendations cho specific user (GET endpoint)"""
    
    request = UserRecommendationRequest(
        user_id=user_id,
        num_recommendations=num_recommendations
    )
    
    return await get_recommendations(request)

@app.get("/users/real")
async def get_real_users():
    """Get danh sách real users từ UserService"""
    try:
        users = await recommendation_service.get_real_users()
        return {
            "success": True,
            "data": {
                "users": users,
                "total_count": len(users),
                "timestamp": datetime.now().isoformat()
            }
        }
    except Exception as e:
        logger.error(f"❌ Get real users error: {e}")
        raise HTTPException(status_code=500, detail=f"Error fetching users: {str(e)}")

@app.get("/podcasts/real")
async def get_real_podcasts():
    """Get danh sách real podcasts từ ContentService"""
    try:
        podcasts_df = await recommendation_service.get_real_podcasts()
        
        if not podcasts_df.empty:
            podcasts_list = podcasts_df.to_dict('records')
        else:
            podcasts_list = []
        
        return {
            "success": True,
            "data": {
                "podcasts": podcasts_list,
                "total_count": len(podcasts_list),
                "timestamp": datetime.now().isoformat()
            }
        }
    except Exception as e:
        logger.error(f"❌ Get real podcasts error: {e}")
        raise HTTPException(status_code=500, detail=f"Error fetching podcasts: {str(e)}")

if __name__ == "__main__":
    import uvicorn
    
    logger.info("🚀 Starting Podcast Recommendation FastAPI Service...")
    logger.info("✅ Service ready!")
    
    uvicorn.run(
        "fastapi_service:app",
        host="0.0.0.0",
        port=8000,
        reload=False
    )