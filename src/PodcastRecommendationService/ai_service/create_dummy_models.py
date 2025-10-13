#!/usr/bin/env python3
"""
Script tạo dummy model files để test service
Chạy nếu chưa có model files từ Kaggle
"""

import pickle
import json
from pathlib import Path
import sys

def create_dummy_model_files():
    """Tạo dummy model files cho testing"""
    
    models_dir = Path("./models")
    models_dir.mkdir(exist_ok=True)
    
    print("🔧 Creating dummy model files for testing...")
    
    # 1. Create dummy mappings
    mappings = {
        'user2user_encoded': {f'user_{i}': i for i in range(100)},
        'podcast2podcast_encoded': {f'p_{i:05d}': i for i in range(200)},
        'userencoded2user': {i: f'user_{i}' for i in range(100)},
        'podcastencoded2podcast': {i: f'p_{i:05d}' for i in range(200)}
    }
    
    with open(models_dir / "mappings.pkl", 'wb') as f:
        pickle.dump(mappings, f)
    print("✅ Created mappings.pkl (100 users, 200 podcasts)")
    
    # 2. Create dummy podcasts data
    import pandas as pd
    
    podcasts_data = []
    categories = ['Career', 'Health', 'Lifestyle', 'Personal_Development', 'Psychology', 'Business', 'Technology']
    
    for i in range(200):
        podcasts_data.append({
            'podcast_id': f'p_{i:05d}',
            'title': f'Bài học {i+1}: Sample Podcast Title',
            'category': categories[i % len(categories)],
            'topics': f'Topic about {categories[i % len(categories)].lower()}',
            'duration_minutes': 15 + (i % 60),
        })
    
    podcasts_df = pd.DataFrame(podcasts_data)
    podcasts_df.to_pickle(models_dir / "podcasts.pkl")
    print(f"✅ Created podcasts.pkl ({len(podcasts_df)} podcasts)")
    
    # 3. Create model metadata
    metadata = {
        "model_info": {
            "name": "collaborative_filtering_model",
            "version": "1.0.0-dummy",
            "type": "collaborative_filtering",
            "framework": "dummy",
            "created_at": "2025-10-10T00:00:00"
        },
        "data_statistics": {
            "num_users": 100,
            "num_podcasts": 200,
            "num_ratings": 5000
        },
        "performance_metrics": {
            "test_mae": 0.7,
            "test_rmse": 0.9
        }
    }
    
    with open(models_dir / "model_metadata.json", 'w') as f:
        json.dump(metadata, f, indent=2)
    print("✅ Created model_metadata.json")
    
    print("\n🎉 Dummy model files created successfully!")
    print(f"📁 Location: {models_dir.absolute()}")
    print("\n⚠️  NOTE: These are dummy files for testing only!")
    print("   Replace with real Kaggle-trained model files for production.")

if __name__ == "__main__":
    try:
        create_dummy_model_files()
        sys.exit(0)
    except Exception as e:
        print(f"❌ Error: {e}")
        sys.exit(1)
