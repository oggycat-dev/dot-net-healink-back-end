# 🚀 Collaborative Filtering Model - Production Ready

## 📊 Model Overview
- **Created**: 2025-10-10 06:16:39
- **TensorFlow Version**: 2.18.0
- **Model Type**: Neural Collaborative Filtering
- **Performance**: Test MAE = 0.6964, Test RMSE = 0.8942

## 📁 Files Included
```
models/
├── collaborative_filtering_model.h5    # 🧠 Main model file
├── mappings.pkl                       # 🗺️ ID mappings (CRITICAL!)
├── podcasts.pkl                       # 🎧 Podcast metadata
├── ratings.pkl                        # ⭐ Ratings data
├── users.pkl                          # 👥 User data
├── model_metadata.json                # 📋 Model information
├── training_history.pkl               # 📈 Training metrics
└── README.md                          # 📖 This file
```

## 🔧 Quick Usage

```python
import tensorflow as tf
import pickle
import numpy as np
import pandas as pd

# Load model components
model = tf.keras.models.load_model('collaborative_filtering_model.h5')
with open('mappings.pkl', 'rb') as f:
    mappings = pickle.load(f)

def predict_rating(user_id, podcast_id):
    """Predict rating for user-podcast pair"""
    
    # Check if IDs exist in training data
    if user_id not in mappings['user2user_encoded']:
        return None, "Unknown user"
    if podcast_id not in mappings['podcast2podcast_encoded']:
        return None, "Unknown podcast"
    
    # Convert to indices
    user_idx = mappings['user2user_encoded'][user_id]
    podcast_idx = mappings['podcast2podcast_encoded'][podcast_id]
    
    # Predict
    prediction = model.predict([np.array([user_idx]), np.array([podcast_idx])])
    rating = float(prediction[0][0])
    
    return rating, "success"

# Example usage
rating, status = predict_rating('user_00001', 'p_00100')
print(f"Predicted rating: {rating:.2f}")
```

## 🎯 Model Statistics
- **Users**: 1,000
- **Podcasts**: 1,990
- **Ratings**: 31,267
- **Sparsity**: 98.43%
- **Parameters**: 489,729

## 🏆 Performance Metrics
- **Test MAE**: 0.6964 (Lower is better)
- **Test RMSE**: 0.8942 (Lower is better)
- **Training Epochs**: 17
- **Best Validation Epoch**: 2

## ⚠️ Important Notes
1. **mappings.pkl is CRITICAL** - Without it, the model cannot be used
2. **Handle unknown IDs** - Check if user/podcast exists in mappings first
3. **Prediction range** - Model outputs ratings between 1.0 and 5.0
4. **Batch predictions** - Model supports batch inference for efficiency

## 🚀 Ready for Production Deployment!
