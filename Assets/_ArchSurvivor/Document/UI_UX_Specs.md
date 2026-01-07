# 🎨 UI/UX SPECIFICATIONS - ARCHSURVIVOR

## **1. UI FRAMEWORK: UI TOOLKIT (UNITY 6)**
Unity 6 là nền tảng chính, tận dụng tối đa `Panel Settings` cho World Space.

### **1.1. World Space UI (WS UI)**
- **Health Bars:** Billboarding (luôn hướng về Camera).
- **Damage Numbers:** Hệ thống Pooling để sinh và thu hồi nhãn số sát thương nhanh chóng.
- **Sorting:** WS UI được render qua một Camera riêng hoặc Overlay Layer để đảm bảo không bị vật thể 3D che khuất một cách bất hợp lý.

---

## **2. LAYOUT DEFINITIONS**

### **2.1. In-Battle HUD**
- **XP Bar:** Sát mép trên màn hình, màu Cyan neon.
- **Kill Count:** Góc trên bên phải, font chữ đậm, có hiệu ứng nảy (Tween) khi tăng số.
- **Skill Button:** Góc dưới bên phải, hiển thị vòng Cooldown mờ dần khi đang hồi chiêu.

### **2.2. Card Drafting Screen**
- **Interaction:** Nhấn vào thẻ để chọn. Nhấn giữ để xem chi tiết chỉ số (Tooltip).
- **Animation:** Thẻ xuất hiện với hiệu ứng lật (Flip) và ánh sáng lấp lánh (Particle) dựa trên độ hiếm.

---

## **3. UX GUIDELINES**
- **Feedback Loop:**
    - Mỗi khi nhặt XP: Hiệu ứng hạt bay về phía thanh XP.
    - Khi nhận Damage: Màn hình rung nhẹ (Camera Shake) và chớp đỏ cạnh màn hình (Vignette).
    - Haptics: Rung ngắt quãng 0.1s khi bắn trúng quái.

---

## **4. ASSET STANDARDS**
- **Icon Size:** 256x256 (Square).
- **Atlas Type:** Sử dụng **Sprite Atlas** để giảm Draw Calls cho UI Metagame.
