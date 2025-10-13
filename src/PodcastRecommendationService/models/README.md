# Model files directory
# Place your trained AI model files here:
#
# collaborative_filtering_model.h5  - The trained TensorFlow/Keras model
# mappings.pkl                      - User and podcast ID mappings
# podcasts.pkl                      - Podcast metadata and information
# ratings.pkl                       - User rating data
#
# These files should be generated from your training script and 
# made available to the AI service either by:
# 1. Copying them during Docker build
# 2. Mounting as a volume in production
# 3. Downloading from cloud storage on startup
#
# Example directory structure:
# /app/models/
# ├── collaborative_filtering_model.h5
# ├── mappings.pkl
# ├── podcasts.pkl
# └── ratings.pkl

README.md