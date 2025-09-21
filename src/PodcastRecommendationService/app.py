import flask
from flask import Flask, request, jsonify
import pickle
import tensorflow as tf
import pandas as pd
import numpy as np
import os

print("--- Bắt đầu khởi tạo Podcast Recommendation Service ---")

# --- 1. Tải các model và dữ liệu cần thiết ---
# Đảm bảo rằng các file này nằm cùng thư mục với file app.py
MODEL_PATH = 'collaborative_filtering_model.h5'
MAPPINGS_PATH = 'mappings.pkl'
PODCASTS_PATH = 'podcasts.pkl'
RATINGS_PATH = 'ratings.pkl'

# Tải model AI
print(f"Đang tải model từ: {MODEL_PATH}")
model = tf.keras.models.load_model(MODEL_PATH)
print("Tải model thành công.")

# Tải các dictionary ánh xạ
print(f"Đang tải mappings từ: {MAPPINGS_PATH}")
with open(MAPPINGS_PATH, 'rb') as f:
    mappings = pickle.load(f)
user2user_encoded = mappings['user2user_encoded']
podcast2podcast_encoded = mappings['podcast2podcast_encoded']
podcastencoded2podcast = mappings['podcastencoded2podcast']
print("Tải mappings thành công.")

# Tải thông tin podcasts và ratings
print(f"Đang tải dữ liệu podcasts từ: {PODCASTS_PATH}")
df_podcasts = pd.read_pickle(PODCASTS_PATH)
print("Tải dữ liệu podcasts thành công.")

print(f"Đang tải dữ liệu ratings từ: {RATINGS_PATH}")
df_ratings = pd.read_pickle(RATINGS_PATH)
print("Tải dữ liệu ratings thành công.")
print("\n--- Service đã sẵn sàng nhận request ---")


# --- 2. Khởi tạo Flask App ---
app = Flask(__name__)


# --- 3. Tạo API Endpoint ---
@app.route('/recommend/<string:user_id>', methods=['GET'])
def recommend(user_id):
    """
    Endpoint chính để lấy gợi ý cho một user_id cụ thể.
    URL Example: http://1227.0.0.1:5001/recommend/user_300
    """
    print(f"\nNhận được request cho user_id: {user_id}")

    # Lấy user_index từ user_id
    user_index = user2user_encoded.get(user_id)
    
    # Kiểm tra nếu user không tồn tại trong dữ liệu huấn luyện
    if user_index is None:
        print(f"Lỗi: UserID {user_id} không có trong dữ liệu.")
        return jsonify({'error': f'User {user_id} not found in the training data.'}), 404

    # Lấy danh sách các podcast mà user đã nghe để loại trừ khỏi gợi ý
    heard_podcasts_df = df_ratings[df_ratings['user_id'] == user_id]
    heard_podcast_indices = [podcast2podcast_encoded.get(p) for p in heard_podcasts_df['podcast_id'] if podcast2podcast_encoded.get(p) is not None]

    # Tạo mảng chứa tất cả các podcast_index
    all_podcast_indices = np.array(list(podcast2podcast_encoded.values()))
    
    # Tạo mảng user_index lặp lại để khớp với số lượng podcast
    user_index_array = np.array([user_index] * len(all_podcast_indices))

    # --- Dự đoán bằng model ---
    print(f"Bắt đầu dự đoán cho {len(all_podcast_indices)} podcasts...")
    predicted_ratings = model.predict([user_index_array, all_podcast_indices]).flatten()
    print("Dự đoán hoàn tất.")

    # Tạo DataFrame từ kết quả dự đoán
    df_predictions = pd.DataFrame({
        'podcast_index': all_podcast_indices,
        'predicted_rating': predicted_ratings
    })

    # Lọc ra những podcast user chưa nghe
    df_predictions = df_predictions[~df_predictions['podcast_index'].isin(heard_podcast_indices)]
    
    # Sắp xếp và lấy top 10 gợi ý
    top_recommendations = df_predictions.sort_values(by='predicted_rating', ascending=False).head(10)
    
    # Lấy podcast_id gốc từ podcast_index
    top_podcast_ids = [podcastencoded2podcast.get(i) for i in top_recommendations['podcast_index']]
    
    # Lấy thông tin chi tiết của các podcast được gợi ý
    results_df = df_podcasts[df_podcasts['podcast_id'].isin(top_podcast_ids)]
    
    # Chuyển kết quả thành dạng JSON để trả về
    recommendations_json = results_df.to_dict(orient='records')

    print(f"Trả về {len(recommendations_json)} gợi ý cho user_id: {user_id}")
    return jsonify(recommendations_json)


# --- 4. Chạy Service ---
if __name__ == '__main__':
    # Chạy Flask app trên cổng 5000
    # Mở trình duyệt và truy cập http://127.0.0.1:5000/recommend/user_300 để thử
    app.run(host='0.0.0.0', port=5001, debug=True)

