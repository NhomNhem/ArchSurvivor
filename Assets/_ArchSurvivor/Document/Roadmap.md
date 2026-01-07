# 🗺️ ARCHSURVIVOR DEVELOPMENT ROADMAP

## **PHASE 1: FOUNDATION & CORE MECHANICS (1-3 WEEKS)**
*Mục tiêu: Hoàn thiện cảm giác điều khiển và chiến đấu cơ bản.*

### **1.1. Technical Setup**
- [ ] Cấu trúc VContainer `GameLifetimeScope` và `ProjectLifetimeScope`.
- [ ] Thiết lập `R3` cho Global Game State (HP, Gold, Experience).
- [ ] Setup `EasySave 3` cho hệ thống Persistence.

### **1.2. Player & Combat**
- [ ] Implement `PlayerController` (Stop-to-Attack logic).
- [ ] Hệ thống `AttackStrategy` sử dụng ScriptableObjects.
- [ ] Base Weapon và Evolution Logic (Tier 1 -> Tier 3).

---

## **PHASE 2: GAMEPLAY SYSTEMS & PROGRESSION (3-6 WEEKS)**
*Mục tiêu: Vòng lặp gameplay hoàn chỉnh trong một màn chơi.*

### **2.1. Card & Drafting System**
- [ ] Database thẻ bài sử dụng CSV hoặc ScriptableObjects.
- [ ] Thuật toán Drafting (Roll Rarity -> Filter Class -> Inject Wishlist).
- [ ] UI Toolkit: Màn hình chọn thẻ (Screen Space).

### **2.2. Enemy & Spawning**
- [ ] Hệ thống `EnemySpawner` dựa trên Waves.
- [ ] AI cơ bản (Chaser, Ranger, Charger).
- [ ] World Space UI: Health Bar và Damage Numbers.

---

## **PHASE 3: METAGAME & UI (6-9 WEEKS)**
*Mục tiêu: Hệ thống nâng cấp vĩnh viễn và UI Lobby.*

### **3.1. Lobby & Management**
- [ ] Scene Lobby: Chọn tướng, thay đổi trang bị.
- [ ] Grimoire Screen: Xem danh sách thẻ bài đã mở khóa.
- [ ] Fusion System: Ghép thẻ bài (với hiệu ứng xác suất v5.0).

### **3.2. Economy & Shop**
- [ ] Currency Service (Gems, Gold, Energy).
- [ ] Gacha System (Chest Opening).
- [ ] Talent Tree (Thiên Phú): Nâng cấp chỉ số vĩnh viễn.

---

## **PHASE 4: POLISH & RELEASE (9-12 WEEKS)**
*Mục tiêu: Chất lượng AAA và sự ổn định.*

### **4.1. Polish & Balance**
- [ ] Hệ thống Boss với 3 Phase Logic.
- [ ] VFX Graph cho chiêu thức đặc biệt và nhát chém.
- [ ] SFX & Haptics (Rung phản hồi).

### **4.2. Optimization & QA**
- [ ] Addressables cho tài nguyên (Loading async).
- [ ] Test trên các thiết bị Mobile thực tế.
- [ ] Final Build & Store Submission.
