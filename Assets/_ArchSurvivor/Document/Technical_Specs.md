# 🛠️ TECHNICAL SPECIFICATIONS - ARCHSURVIVOR

## **1. ARCHITECTURE OVERVIEW**
Game sử dụng kiến trúc **Dependency Injection (VContainer)** kết hợp với **Reactive Programming (R3)**.

### **1.1. DI Hierarchy**
- **ProjectLifetimeScope:** Quản lý Global Services (Save, Audio, Localization, Network).
- **HomeLifetimeScope (Lobby):** Quản lý Metagame Services (Shop, Inventory, Card Fusion).
- **BattleLifetimeScope (Gameplay):** Quản lý Combat Services (Spawner, DamageCalc, XPService).

### **1.2. Data Flow (Reactive)**
Sử dụng `R3` để tránh `Update()` hell:
- `ReactiveProperty<int> PlayerHP` -> Bind trực tiếp vào UI Toolkit Label/Progress Bar.
- `Subject<DamageEvent> DamageEmitter` -> Dùng để trigger VFX/SFX và trừ máu.

---

## **2. DATA MANAGEMENT**
- **ScriptableObjects:** Dùng cho Static Data (Class stats, Card definitions, Boss phases).
- **EasySave 3:** Toàn bộ Save Data được mã hóa và lưu trữ Key-Value.
    - Key: `GameProgress_Cards`, `Player_Inventory`, `Settings`.

---

## **3. UI TOOLKIT STANDARDS**
- **USS (CSS):**
    - Sử dụng biến toàn cục cho Palette: `--color-primary`, `--color-gold`, `--font-size-standard`.
    - Class naming: `.btn-primary`, `.card-panel`, `.hud-bar`.
- **UI Logic:**
    - Mỗi màn hình UI có 1 `Presenter` (C#) để nhận data từ Service và cập nhật `VisualElement`.

---

## **4. COMBAT LOGIC**
### **Damage Calculation**
```csharp
public int CalculateDamage(Entity attacker, Entity defender) {
    float raw = (attacker.BaseATK + attacker.BonusATK) * attacker.Multiplier;
    if (Random.value < attacker.CritRate) raw *= attacker.CritDmg;
    return Mathf.Max(1, Mathf.RoundToInt(raw - defender.Defense));
}
```
### **Boss Phase Logic**
Sử dụng **State Pattern** cho Boss AI:
- `IdleState` -> `AttackState` -> `PhaseTransitionState` (khi máu < 50%) -> `EnragedState`.
