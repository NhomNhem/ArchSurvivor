# ⚔️ PROJECT: ARCHSURVIVOR (SHATTERED ECHO OF AETHERIA)

## GAME DESIGN DOCUMENT (GDD) — MASTER VERSION 2.0
Tài liệu thiết kế toàn diện, chứa đầy đủ thông tin từ Core Gameplay, Lore, Math Model đến UI/UX Spec.

---

## 📌 MỤC LỤC (TABLE OF CONTENTS)

- [1. Tổng Quan Dự Án (Executive Summary)](#1-tổng-quan-dự-án-executive-summary)
- [2. Cốt Truyện & Thế Giới (Lore & World Building)](#2-cốt-truyện--thế-giới-lore--world-building)
- [3. Gameplay & Vòng Lặp (Game Loops)](#3-gameplay--vòng-lặp-game-loops)
  - [3.1 Micro-Loop](#31-micro-loop)
  - [3.2 Macro-Loop](#32-macro-loop)
  - [3.3 Controls](#33-controls)
- [4. Hồ Sơ Anh Hùng Chi Tiết (Hero Dossiers)](#4-hồ-sơ-anh-hùng-chi-tiết-hero-dossiers)
  - [4.1 Quy chuẩn Chỉ số Khởi đầu](#41-quy-chuẩn-chỉ-số-khởi-đầu)
  - [4.2 Chi tiết 7 Class](#42-chi-tiết-7-class)
- [5. Hệ Thống Thẻ Bài & Kỹ Năng (Grimoire & Cards System)](#5-hệ-thống-thẻ-bài--kỹ-năng-grimoire--cards-system)
  - [5.1 Cơ chế Draft](#51-cơ-chế-draft)
  - [5.2 Danh sách Thẻ Bài (Tóm tắt)](#52-danh-sách-thẻ-bài-tóm-tắt)
  - [5.3 Cơ chế Soul Weapon](#53-cơ-chế-soul-weapon)
- [6. Hệ Thống Kẻ Thù & Loot Table (Enemies & Drops)](#6-hệ-thống-kẻ-thù--loot-table-enemies--drops)
- [7. Sự Kiện & Chu Kỳ Thời Gian (Time-Gated Content)](#7-sự-kiện--chu-kỳ-thời-gian-time-gated-content)
- [8. Giao Diện Người Dùng (UI/UX Design)](#8-giao-diện-người-dùng-uiux-design)
- [9. Kiến Trúc Kỹ Thuật (Technical Specifications)](#9-kiến-trúc-kỹ-thuật-technical-specifications)
- [10. Cơ Chế Toán Học & Cân Bằng (Mechanics & Math)](#10-cơ-chế-toán-học--cân-bằng-mechanics--math)
- [11. Hướng Dẫn Thiết Kế UI (Figma Specs)](#11-hướng-dẫn-thiết-kế-ui-figma-specs)

---

## 1. TỔNG QUAN DỰ ÁN (EXECUTIVE SUMMARY)

### 1.1 Giới thiệu (Elevator Pitch)

ArchSurvivor là một tựa game hành động sinh tồn (Survival Roguelite / Bullet Heaven) lấy cảm hứng từ Vampire Survivors và Brotato. Điểm khác biệt cốt lõi: tập trung vào Class Identity và hệ thống xây dựng bộ kỹ năng qua thẻ bài (Grimoire Cards). Người chơi điều khiển 1 trong 7 anh hùng, chiến đấu chống hàng nghìn quái vật để khôi phục "Shattered Echo of Aetheria".

### 1.2 Thông tin cơ bản

- Tên dự án: ArchSurvivor (Working Title)
- Tên cốt truyện: Shattered Echo of Aetheria
- Thể loại: Roguelite, Bullet Heaven, Survival, RPG
- Góc nhìn: Top-down 2D
- Nền tảng: PC (Steam), Mobile (Android / iOS)
- Đối tượng: Hardcore (theory-crafting) + Casual (power fantasy)

### 1.3 Điểm Bán Hàng Độc Nhất (USP)

- The Grimoire Choice: Draft thẻ bài với cơ chế trọng số (Class Weighting) và ô cấm thuật (Forbidden Slot).
- Soul Weapon System: Vũ khí tiến hoá dựa trên Soul Shards.
- High-Octane Tech Stack: ECS/Reactive (R3, VContainer) để xử lý hàng nghìn quái vật.
- Time-Gated Lore: Boss tuần xuất hiện theo ngày trong tuần, gắn cốt truyện.

---

## 2. CỐT TRUYỆN & THẾ GIỚI (LORE & WORLD BUILDING)

### 2.1 Tiền đề (Synopsis)

Lục địa Aetheria được bảo hộ bởi "Trái Tim Nguyên Sơ". Khi nó vỡ, 6 mảnh rơi xuống 6 vương quốc, biến cư dân thành quái vật. Người chơi là "Echo Walkers" có khả năng cộng hưởng với mảnh vỡ để thanh tẩy thế giới.

### 2.2 Lục Quốc (The Six Realms - Stages)

Mỗi màn tương ứng một vương quốc với môi trường, quái và tài nguyên đặc trưng:

- Iron Bastion — Thành trì đổ nát; quái là giáp sắt đi bộ.
- Verdant Weald — Rừng đột biến; quái hệ độc / mộc.
- Void Sands — Sa mạc ảo ảnh; quái tàng hình / phân tách.
- Frost Spire — Đỉnh núi băng; bão tuyết làm chậm.
- Undercity — Thành phố ngầm steampunk; quái máy móc / hơi nước.
- Chaos Rift — Cõi hỗn mang, trùm cuối, tổng hợp mọi loại quái.

---

## 3. GAMEPLAY & VÒNG LẶP (GAME LOOPS)

Core Loop chia làm Micro (trận chơi) và Macro (meta progression).

### 3.1 Micro-Loop (Vòng lặp chiến đấu — ~15 phút)

1. Select: Chọn Hero & trang bị khởi đầu.
2. Spawn: Bắt đầu Level 1 với vũ khí cơ bản.
3. Fight: Tiêu diệt quái để nhận EXP Gems.
4. Draft: Lên cấp -> Game tạm dừng -> Chọn 1/3 thẻ Grimoire.
5. Evolve: Thu thập Soul Shards từ Elite để tiến hoá vũ khí.
6. Result: Thắng -> Vàng + Vật phẩm hiếm + Mở map; Thua -> Nhận vàng ít hơn.

### 3.2 Macro-Loop (Vòng lặp tiến hoá — Meta Game)

- Resource Management: Dùng vàng để mở khóa chỉ số vĩnh viễn (Talent Tree).
- Crafting: Dùng vật phẩm hiếm để nâng cấp Artifacts.
- Hero Unlock: Mở khóa nhân vật mới.
- Weekly Challenge: Tham gia Boss tuần để săn Soul Stone.

Tóm tắt: Fight Map → Get Loot → Upgrade Artifacts → Fight Harder Map → Weekly Boss → Unlock rewards.

### 3.3 Cơ chế điều khiển (Controls)

PC:
- Di chuyển: WASD hoặc mũi tên
- Tấn công: Auto-fire
- Ultimate: Spacebar hoặc chuột phải
- Menu / Pause: ESC

Mobile:
- Di chuyển: Joystick ảo trái
- Tấn công: Auto-fire
- Ultimate: Nút ảo bên phải

---

## 4. HỒ SƠ ANH HÙNG CHI TIẾT (HERO DOSSIERS)

### 4.1 Quy chuẩn Chỉ số Khởi đầu (Base Stat Standards)

(Standards dùng để cân bằng; Warrior là đơn vị chuẩn)

- Standard HP: 100
- Standard Speed: 5.0 (Unity units/sec)
- Standard Damage: 10

### 4.2 Chi tiết 7 Class

Lưu ý: giữ nguyên tên, vai trò và cơ chế đã mô tả. Tóm tắt dưới dạng rõ ràng:

#### A. THE BULWARK — KNIGHT (Ser Alric)
- Vai trò: Tanker / Phản sát thương
- Visual: Giáp nặng, khiên lớn, màu bạc & xanh hoàng gia
- Stats:
  - MaxHP: 150
  - MoveSpeed: 4.0
  - Armor: 20
- Passive: Iron Will — Mỗi 10% máu mất đi tăng 5 Armor
- Weapon: Rusty Sword — Đánh quét ngang (Arc) đẩy lùi quái

#### B. THE RAVAGER — BARBARIAN (Krog)
- Vai trò: High Risk / AOE
- Stats:
  - MaxHP: 120
  - Damage Multiplier: 1.2x
  - HealthRegen: 0.5/sec
- Passive: Undying Rage — Tăng 1% Damage cho mỗi 1% máu đã mất
- Weapon: Whirlwind Axe — Xoay rìu 360°

#### C. THE DEA — RANGER (Elara)
- Vai trò: Glass Cannon / Single Target DPS
- Stats:
  - MaxHP: 70
  - MoveSpeed: 6.5
  - CritChance: 10%
  - Range: +50%
- Passive: Eagle Eye — Nếu 3s không nhận sát thương, đòn tiếp theo x2 Damage
- Weapon: Spirit Bow — Tên xuyên 2 mục tiêu

#### D. THE ELEMENTALIST — MAGE (Vex)
- Vai trò: Crowd Control / Area Damage
- Stats:
  - MaxHP: 80
  - CooldownReduction: 10%
  - PickupRange: +100%
- Passive: Mana Overflow — Nhặt EXP Gem có 20% tỉ lệ nổ đẩy lùi quái
- Weapon: Arcane Missiles — 3 tia năng lượng tự tìm

#### E. THE SHADE — ROGUE (Nyx)
- Vai trò: Evasion / Poison / Crit
- Stats:
  - MaxHP: 90
  - MoveSpeed: 6.0
  - Dodge: 15%
- Passive: Shadow Step — Sau khi né thành công, tăng 100% Crit Rate trong 2s
- Weapon: Venom Dagger — Gây Poison (DoT)

#### F. THE SAINT — CLERIC (Oria)
- Vai trò: Sustain / Holy Aura
- Stats:
  - MaxHP: 110
  - HealthRegen: 1.0/sec
  - Damage: 0.8x
- Passive: Divine Grace — Hồi máu vượt tối đa chuyển thành Shield
- Weapon: Holy Smite — Vòng ánh sáng gây sát thương liên tục

#### G. THE ARTIFICER — ENGINEER (Torb)
- Vai trò: Summoner / Stationary Defense
- Stats:
  - MaxHP: 100
  - Engineering: 10
  - MoveSpeed: 4.5
- Passive: Scrap Metal — Quái chết rớt ốc vít, nhặt hồi máu cho trụ
- Weapon: Sentry Turret — Đặt trụ (Max 3)

---

## 5. HỆ THỐNG THẺ BÀI & KỸ NĂNG (GRIMOIRE & CARDS SYSTEM)

### 5.1 Cơ chế Draft (The Grimoire Choice)

- Khi lên cấp: hiển thị 3 thẻ bài để chọn.
- Tỉ lệ xuất hiện: Common 60% | Rare 30% | Epic/Exclusive 10%
- Forbidden Slot: 5% cơ hội xuất hiện thẻ "Cấm thuật" (High Risk / High Reward)

### 5.2 Danh sách Thẻ Bài (Tóm tắt)

A. Thẻ Chỉ Số Chung (Common) — ví dụ:

| ID | Tên | Effect |
|---|---:|---|
| COM_HP_1 | Vitality I | Tăng Max HP 10% |
| COM_ATK_1 | Strength I | Tăng Damage 10% |
| COM_SPD_1 | Haste I | Tăng AtkSpeed 10% |
| COM_MOV_1 | Swiftness | Tăng MoveSpeed 10% |
| COM_DEF_1 | Iron Skin | Giảm 5 DMG nhận vào |
| COM_ECO_1 | Greed | Tăng 50% Gold |

B. Thẻ Cơ Chế Vũ Khí (Mechanic) — ví dụ:

| ID | Tên | Loại | Mô tả |
|---|---:|---|---|
| RNG_MULTI | Multishot | Ranged | Bắn thêm 1 tia |
| RNG_PIERCE | Piercing | Ranged | Đạn xuyên 1 kẻ |
| RNG_RICO | Ricochet | Ranged | Đạn nảy sang 2 mục tiêu |
| MEL_GIANT | Giant Swing | Melee | Tăng tầm cận 30% |
| MEL_BLOOD | Blood Explode | Melee | Quái chết nổ gây dmg |

C. Thẻ Độc Quyền Class (Exclusive) — chỉ xuất hiện nếu đúng Class.

| ID | Tên | Hero | Rarity | Mô tả |
|---|---:|---|---:|---|
| KNI_BASH | Shield Bash | Knight | Platinum | Lướt gây 300% dmg & Choáng |
| KNI_PARRY | Parry | Knight | Gold | Tự động chặn 1 đòn/10s |
| KNI_EXCAL | Excalibur | Knight | Diamond | Sóng năng lượng xuyên bản đồ |
| BAR_ZERK | Berzerk | Barbarian | Diamond | HP thấp → Damage tăng (max x3) |
| BAR_SPIN | Spin To Win | Barbarian | Platinum | Xoay rìu dài + tăng tốc |
| ARC_SNIPE | Sniper | Ranger | Platinum | Đứng yên 2s → chắc Crit x2 |
| ARC_KITE | Kiting Master | Ranger | Gold | Sau bắn, tăng 20% tốc trong 1s |
| ROG_EXEC | Assassinate | Rogue | Diamond | Kết liễu quái thường dưới 15% HP |
| CLR_REZ | Second Life | Cleric | Diamond | Hồi sinh 1 lần/màn 50% HP |

> Ghi chú: danh sách đầy đủ lấy từ GameData.xlsx — giữ nguyên dữ liệu nguồn trong pipeline import (ScriptableObjects / CSV).

### 5.3 Cơ chế Soul Weapon (Tiến hoá vũ khí)

- Thu thập: Elite rớt Soul Shard.
- Kích hoạt: 3 Shards + vũ khí Lv.8 → Mở Rương → Tiến hoá.
- Kết quả: Vũ khí đổi hình dạng, thêm VFX, đổi logic (ví dụ: dao găm → tự tìm mục tiêu).

---

## 6. HỆ THỐNG KẺ THÙ & LOOT TABLE (ENEMIES & DROPS)

### 6.1 Phân loại kẻ thù

- Fodder: Máu thấp (dơi, skeleton)
- Rusher: Nhanh, cảm tử (sói, golem lăn)
- Tanker: Trâu, chậm (giáp sắt, ent)
- Ranger: Bắn xa (phù thủy, cung thủ)
- Elite: Có vòng sáng, rớt Rương / Soul Shard

### 6.2 Loot & Tài nguyên theo Map

Mỗi map rớt nguyên liệu riêng để khuyến khích luân phiên chơi map.

| Map | Chủ đề | Nguyên liệu (Common) | Nguyên liệu (Rare) | Công dụng |
|---|---|---:|---:|---|
| Iron Bastion | Sắt thép | Scrap Metal | Titanium Core | Nâng cấp Vũ khí (DMG) |
| Verdant Weald | Rừng rậm | Mutated Root | Life Essence | Nâng cấp Giáp (HP) |
| Void Sands | Sa mạc | Mirage Dust | Eye of Horus | Nâng cấp Giày (Speed) |
| Frost Spire | Băng giá | Permafrost | Frozen Heart | Nâng cấp Nhẫn (Crit) |
| Undercity | Steampunk | Gearspring | Aether Battery | Nâng cấp Auto-bot |
| Chaos Rift | Hỗn mang | Chaos Residue | Void Stone | Đột phá giới hạn Level |

---

## 7. SỰ KIỆN & CHU KỲ THỜI GIAN (TIME-GATED CONTENT)

### 7.1 Cốt truyện: Aether Fluctuation

Cổng không gian mở theo tần số dao động của ngày trong tuần.

### 7.2 Lịch Boss Tuần (Weekly Boss)

| Thứ | Boss | Hiệu ứng Map | Phần thưởng |
|---|---|---|---|
| Thứ 2 | Slime King | Map không hồi máu | x50 Gems |
| Thứ 3 | Iron Golem | Quái +50% Vật lý | Mảnh Thẻ Knight |
| Thứ 4 | Wind Assassin | Player chậm 30% | Mảnh Thẻ Rogue |
| Thứ 5 | Storm Dragon | Sét đánh ngẫu nhiên | Mảnh Thẻ Mage |
| Thứ 6 | Succubus | Đảo ngược nút đi | Ticket quay tướng |
| Thứ 7 | Lich Lord | Máu giảm dần | Nguyên liệu Tối thượng |
| CN | Boss Rush | Đấu 3 Boss | x2 Tài nguyên |

---

## 8. GIAO DIỆN NGƯỜI DÙNG (UI/UX DESIGN)

### 8.1 Main Menu

- Start Game (Nổi bật)
- Heroes (Chọn / Nâng cấp)
- Armory (Chế tạo / Lắp trang bị)
- Grimoire (Bộ sưu tập thẻ)
- Shop / Settings

### 8.2 HUD (Trong game)

- Góc trên trái: Avatar, HP Bar (Đỏ), EXP Bar (Vàng), Level
- Góc trên phải: Đồng hồ, Kill Count, Gold
- Góc dưới: Danh sách thẻ Passive / Active (Icon nhỏ)
- Damage Text: Trắng (thường), Vàng (crit), Xanh lá (heal)

### 8.3 Level Up Screen

- Game tạm dừng
- 3 thẻ hiển thị (xếp dọc/ngang), nút Reroll, Skip
- Mỗi thẻ: Tên, Icon, Rarity (Màu viền), Mô tả chi tiết

---

## 9. KIẾN TRÚC KỸ THUẬT (TECHNICAL SPECIFICATIONS)

### 9.1 Tech Stack

- Engine: Unity 2020.3 LTS+ (hoặc Unity 6000.3 theo ghi chú nội bộ)
- Architecture: MVRP (Model-View-Reactive-Presenter)
- DI: VContainer
- Reactive: R3
- Async: UniTask
- Pooling: Custom Pool cho Projectiles / Enemies

### 9.2 Data Management

- Dữ liệu thẻ/chỉ số: ScriptableObject import từ CSV / JSON
- Asset management: Addressables

---

## 10. CƠ CHẾ TOÁN HỌC & CÂN BẰNG (MECHANICS & MATH)

### 10.1 Công thức Leveling (EXP Curve)

Formula (XP required):

```math
XP_Required = 100 * (CurrentLevel)^{2.2}
```

Ví dụ:
- Lv 1 → 2: 100 XP
- Lv 10 → 11: ~15,800 XP

### 10.2 Stacking Rules

- Additive (+): Các thẻ Common (ví dụ Strength I, Vitality I)
- Multiplicative (×): Crit Dmg, Buff đặc biệt

```math
Damage = (BaseDmg * (1 + sum(%Additive))) * (product(Multipliers))
```

### 10.3 Armor & Defense

```math
DamageReduction = Armor / (Armor + 100)
```

Ví dụ: Armor = 100 → DamageReduction = 50%.

### 10.4 Cooldown Calculation

```math
RealCooldown = BaseCooldown / (1 + (AtkSpeed% / 100))
```

Lưu ý: cooldown không bao giờ về 0.

### 10.5 Enemy Scaling

```math
EnemyHP = BaseHP * (1 + (Minute)^{1.5} * 0.5)
```

Gợi ý: tăng dần sức mạnh quái theo thời gian (ví dụ phút 20 → ~45× so với phút 1 theo biểu thức).

---

## 11. HƯỚNG DẪN THIẾT KẾ UI (FIGMA SPECS)

### 11.1 Color Palette

- Primary Action: Vàng kim (Gold)
- Danger / HP: Đỏ thẫm (Crimson)
- EXP / Mana: Xanh dương (Royal Blue)
- Rarity:
  - Common: Bạc / Xám
  - Rare: Vàng
  - Epic: Tím
  - Exclusive / Legendary: Holographic / Đỏ rực

### 11.2 Components

- Health Bar: chia vạch (mỗi vạch 100 HP)
- Card Template: Khung chung — thay đổi màu viền & Icon
- Joystick: Trong suốt 50%, hiển thị khi chạm
- Damage Numbers: Font đậm, Stroke đen để nổi bật

---

> Phiên bản này là Master Version 2.0 — mọi sửa đổi tiếp theo xin cập nhật trực tiếp vào file này.
