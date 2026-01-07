Tài liệu này quy định các tiêu chuẩn lập trình để đảm bảo code sạch, dễ bảo trì và tối ưu trên **Unity 6**.

> [!IMPORTANT]
> **COLLABORATION CONTRACT:** File này phục vụ như một bản "Brief" giữa AI và User. AI sẽ dựa vào các quy tắc này để hướng dẫn, gợi ý và viết code, đảm bảo người dùng học được tư duy và kỹ thuật của một **Senior Engineer**. Mọi đề xuất mới từ AI hoặc yêu cầu từ User nếu thay đổi kiến trúc đều phải được cập nhật vào đây trước khi thực hiện.

---

## **1. C# CODING STANDARDS**
- **Naming Conventions (Code):**
    - `ClassNames`, `MethodNames`: PascalCase.
    - `_privateFields`: camelCase với tiền tố gạch dưới.
    - `publicFields`, `Properties`: PascalCase.
    - `localVariables`: camelCase.
- **Naming Conventions (Assets):**
    - `PRE_`: Prefab (ví dụ: `PRE_Knight_Hero`).
    - `TEX_`: Texture.
    - `MAT_`: Material.
    - `SFX_` / `BGM_`: Audio.
    - `VFX_`: Visual Effects.
    - `SO_`: ScriptableObject.
- **Modern C#:**
    - Ưu tiên sử dụng `Primary Constructors` (Unity 6 / C# 12).
    - Sử dụng `var` khi kiểu dữ liệu đã rõ ràng.

---

## **2. UNITY & PERFORMANCE**
- **Zero Update Policy:**
    - Hạn chế tối đa sử dụng `void Update()`.
    - Sử dụng **R3 (Reactive)** để lắng nghe thay đổi dữ liệu hoặc **UniTask** cho các vòng lặp cần thiết.
- **Asynchronous:**
    - Luôn dùng `UniTask` thay cho `Coroutine` hoặc `Task`.
    - Thêm `PlayerLoopTimer` khi cần delay trong logic gameplay.
- **R3 Lifecycle:**
    - Bắt buộc sử dụng `CompositeDisposable` hoặc `AddTo(this)` để quản lý vòng đời của các Subscription, tránh memory leak.

---

## **3. DEPENDENCY INJECTION (VCONTAINER)**
- **No Singletons:** Tuyệt đối không tự viết `Singleton` class. Tất cả service phải được đăng ký qua `VContainer`.
- **Injection:**
    - Ưu tiên `Constructor Injection`.
    - Chỉ dùng `[Inject]` field/property cho MonoBehaviors (View).
- **Registration:**
    - UI và Scene Logic đăng ký tại `LifetimeScope` của Scene đó.
    - Core Services đăng ký tại `ProjectLifetimeScope`.
- **EntryPoints (Quan trọng):**
    - Hạn chế code logic trong `Start()` hay `Update()` của MonoBehaviour.
    - Sử dụng `IStartable`, `IInitializable`, `ITickable`, `IFixedTickable` của VContainer để quản lý logic thực thi của Service.

---

### **3.3. Scoping Strategy**
- **ProjectLifetimeScope (Global):** Chứa các Service tồn tại vĩnh viễn (Save, Audio, Input, Network).
- **GameLifetimeScope (Scene):** Chứa Logic đặc thù của màn chơi (Player Movement, Enemy AI, XP System). Toàn bộ Logic này sẽ bị hủy khi thoát cảnh để giải phóng RAM.
- **Tư duy Senior:** Luôn đặt câu hỏi: "Service này có cần tồn tại ở mọi nơi không?". Nếu không, hãy đẩy nó xuống Scene Scope.

---

## **4. FOLDER & ARCHITECTURE**
- **Modular Structure:** Code phải nằm trong `Assets/_ArchSurvivor/Features/` theo từng tính năng.
- **Scripts Organization:**
    - `Models`: Chứa dữ liệu (POCO) và SO.
    - `Views`: MonoBehaviors điều khiển Visual (UI Toolkit / Animancer).
    - `Presenters`: Logic trung gian kết nối Model và View.
    - `Services`: Core logic xử lý data/system.

---

## **5. RESOURCE MANAGEMENT**
- **Addressables:** Toàn bộ Prefab, Audio, Visual Assets phải được load qua Addressables. Không kéo thả trực tiếp vào Inspector trừ khi là data tĩnh trong SO.

---

## **6. GIT & DOCUMENTATION**
- **Commit Message:** Ghi rõ tính năng vừa làm (ví dụ: `feat: add player movement`, `fix: combat damage calc`).
- **Update Docs:** Khi thay đổi logic lớn, phải cập nhật tương ứng vào `GDD.md` hoặc `Technical_Specs.md`.

---

## **7. THIRD-PARTY ASSETS RULES**

### **7.1. InitArgs (Sisus.Init)**
- **Safe Initialization:** Toàn bộ MonoBehaviour cần nhận tham số khi khởi tạo (Instantiate) phải sử dụng `IInitializable<>`.
- **Avoid Nulls:** Không sử dụng `Awake` để tìm kiếm dependency. Luôn truyền data qua `Init` để đảm bảo State của Object luôn hợp lệ ngay khi vừa sinh ra.

### **7.2. EasySave 3 (ES3)**
- **Centralized Service:** Chỉ truy cập ES3 thông qua `SaveService`. Không gọi trực tiếp `ES3.Save` rải rác trong code feature.
- **Encryption:** Luôn bật `Encryption` cho dữ liệu liên quan đến tiền tệ (Gold/Gems).
- **Auto Save:** Chỉ thực hiện Save khi kết thúc màn chơi hoặc thoát game (để tối ưu IO).

### **7.3. DOTween Pro**
- **Lifecycle Management:** Luôn gọi `.SetLink(gameObject)` cho mọi Tween để tự động Kill khi Object bị Destroy, tránh Leak bộ nhớ.
- **Efficiency:** Không dùng Tween cho các logic tính toán nặng. Chỉ dùng cho Visual feedback (Scale, Alpha, Movement UI).
- **Clean Code:** Ưu tiên dùng `DOTweenPath` hoặc `DOTweenAnimation` cho các chuyển động đơn giản để giảm code C#.

### **7.4. Animancer Pro**
- **State Definition:** Thay thế hoàn toàn Animator Controller truyền thống. Quản lý Animation qua Class hoặc ScriptableObject.
- **Transition Logic:** Sử dụng `AnimancerComponent.Play()` kết hợp với `Fade` để chuyển đổi mượt mà giữa các State (Idle -> Run -> Attack).
- **Layering:** Tận dụng hệ thống Layer của Animancer để hòa trộn Animation (ví dụ: chân chạy nhưng tay chém).

---

## **8. INPUT SYSTEM (NEW)**

### **8.1. Input Action Assets**
- **No Hardcoding:** Tuyệt đối không hardcode phím nhấn trong C#. Toàn bộ Input phải thông qua `.inputactions` asset.
- **Type Safety:** Bật tính năng **Generate C# Class** cho Input Action Asset để sử dụng các Wrapper class có sẵn, tránh dùng chuỗi string để tìm Action.

### **8.2. Input Architecture**
- **Input Service:** Luôn wrap logic Input vào một `InputService`. Service này sẽ lắng nghe sự kiện từ Input Action Asset và emit ra thông qua **R3 (Reactive)**.
    - Ví dụ: `public ReadOnlyReactiveProperty<Vector2> MovementInput { get; }`
- **Decoupling:** Player Controller không được truy cập trực tiếp vào lớp Input của Unity. Nó chỉ được phép "sub" vào các stream dữ liệu từ `InputService`.
- **Joystick Integration:** Toàn bộ Joystick UI (Floating/Fixed) phải được liên kết với `InputService`. Giá trị từ Joystick (Vector2) phải được chuẩn hóa (normalized) trước khi truyền vào Flow xử lý của Nhân vật.
- **UI Support:** Sử dụng `InputSystemUIInputModule` cho UI Toolkit để đảm bảo đồng bộ giữa điều khiển bằng phím và chạm màn hình.

---

## **9. RECOMMENDED PACKAGES & UTILS**

### **9.1. MessagePipe**
- **Pub/Sub Logic:** Sử dụng MessagePipe để truyền tin giữa các Module không liên quan trực tiếp đến nhau (ví dụ: `AchievementService` lắng nghe sự kiện `EnemyDied`).
- **Performance:** Ưu tiên dùng `IPublisher<T>/ISubscriber<T>` thay vì EventBus truyền thống để tận dụng hiệu năng vượt trội và tính linh hoạt của VContainer.

### **9.2. ZString (Zero-allocation String)**
- **UI Optimization:** Khi hiển thị các giá trị thay đổi liên tục (XP, Damage numbers, Timer), bắt buộc sử dụng `ZString` để tránh sinh ra Garbage Collector (GC) từ các phép cộng chuỗi.
    - Ví dụ: `textElement.text = ZString.Format("XP: {0}/{1}", current, max);`

### 9.3. BakingSheet (Data Pipeline)
- **Source of Truth:** Toàn bộ chỉ số cân bằng (Balancing) phải nằm trong file Excel/Google Sheets và được import qua BakingSheet.
- **Type-Safe Access:** Không dùng string ID. Sử dụng code-generated classes từ BakingSheet để truy cập dữ liệu (ví dụ: `sheets.Enemies[EnemyId.Slime]`).
- **Verificators:** Tận dụng tính năng `Verificator` của BakingSheet để bắt lỗi logic ngay khi import dữ liệu (ví dụ: kiểm tra HP không được âm, ID không được trùng).

---

## **10. DATA MANAGEMENT PIPELINE**
- **Data over Logic:** Ưu tiên thiết kế game theo hướng tham số hóa. Thay vì viết 10 cái Script cho 10 loại quái, hãy viết 1 Script "GenericEnemy" và điều khiển hành vi của nó hoàn toàn bằng Data từ BakingSheet.
- **Iteration Loop:** Quy trình thay đổi chỉ số phải là: **Sửa Excel -> Click Import -> Test**. Tuyệt đối không sửa số trực tiếp trong Prefab hoặc Script nếu số đó thuộc về Master Data.
- **Localization:** Toàn bộ text hiển thị (Name, Description) phải được quản lý tập trung trong file dữ liệu, không hardcode string trong UI Toolkit hay C#.

---

## **11. UI TOOLKIT OPTIMIZATION**
- **Panel Settings:** Chỉ dùng 1-2 `Panel Settings` cho toàn bộ game để tối ưu Draw Calls.
- **Usage Hints:** Cấu hình `UsageHints` (Dynamic Transform, Group Transform) cho các phần tử UI di chuyển hoặc thay đổi thường xuyên.
- **Data Binding:** Sử dụng kiến trúc "Push" (Data thay đổi -> notify UI) thay vì "Pull" (UI tự check data mỗi frame).

---

## **12. TESTING & QUALITY ASSURANCE**
- **Logic Testing:** Các Business Logic (tính dame, gộp thẻ) phải được viết dưới dạng Plain C# Classes để có thể viết **Unit Test** mà không cần chạy Scene.
- **Validation:** Sử dụng `Assert` hoặc `RequireComponent` để bắt lỗi sớm ngay trong Editor.

---

## **13. PERFORMANCE: OBJECT POOLING**
- **No Instantiate/Destroy in Loop:** Tuyệt đối không dùng `Instantiate` hay `Destroy` cho các vật thể xuất hiện nhiều lần (Đạn, Quái, Số sát thương).
- **Unity 6 ObjectPool:** Sử dụng hệ thống `UnityEngine.Pool` có sẵn để quản lý vòng đời của Entity.
- **Warmup:** Thực hiện "nạp sẵn" (Warmup) một lượng Object cần thiết ngay khi load Scene để tránh giật lag (Spike) lúc đang chiến đấu.

---

## **14. SCENE & LOADING POLICY**
- **Async Only:** Sử dụng `SceneManager.LoadSceneAsync` kết hợp với `UniTask` để load scene không làm treo game.
- **Loading Screen:** Toàn bộ việc chuyển scene phải qua một `LoadingService`. Service này sẽ quản lý thanh Progress bar và các câu tip hiển thị cho người chơi.
- **Additive Loading:** Cân nhắc dùng Additive Scene cho UI HUD hoặc các hệ thống Global để tiết kiệm thời gian load lại mỗi khi vào trận.

---

## **15. CORE SYSTEMS IMPLEMENTATION (BRIEF)**

### **15.1. Audio System**
- **Architecture:** `AudioService` tập trung, đăng ký qua VContainer.
- **Resources:** Sử dụng **Addressables** để load AudioClips theo yêu cầu (on-demand).
- **Implementation:** 
    - Sử dụng **AudioMixer** để quản lý Group (BGM, SFX, UI).
    - Hệ thống Pooling cho `AudioSource` để tránh tạo/xóa GameObject liên tục.

### **15.2. Camera Logic**
- **Framework:** **Cinemachine** (mặc định cho Senior).
- **Features:** 
    - `CinemachineVirtualCamera` cho Follow Player.
    - `CinemachineImpulse` cho hiệu ứng Rung (Screen Shake).
    - Custom code để giới hạn Camera trong biên (Bounding Box) của Dungeon.

### **15.3. Animation & FSM**
- **Animation Framework:** **Animancer Pro** (loại bỏ Animator Controller).
- **State Logic (FSM):** 
    - Sử dụng **Code-based State Pattern**. Mỗi trạng thái (Idle, Move, Attack, Dead) là một Class riêng kế thừa từ BaseState.
    - Không lạm dụng Animator Parameters (bool, trigger). Logic chuyển trạng thái hoàn toàn nằm trong C#.
- **Transitions:** Sử dụng `Fade` của Animancer để hòa trộn animation mượt mà.

### **15.4. VFX (Visual Effects)**
- **Unity 6 Power:** Ưu tiên sử dụng **VFX Graph** cho các hiệu ứng sương mù, tia lửa, và các đòn đánh diện rộng (AOE) vì nó chạy trên GPU, cực kỳ tối ưu khi có nhiều quái vật.
- **Secondary:** Dùng **Particle System** truyền thống cho các hiệu ứng đơn giản cần tương tác vật lý nhẹ.

### **15.5. AI & Navigation (Hybrid Approach)**
- **Problem:** `NavMeshAgent` mặc định rất nặng bài toán CPU khi có >100 quái vật vì hệ thống Avoidance (né nhau) rất phức tạp.
- **Senior Solution:**
    - **Spatial Data:** Sử dụng **AI Navigation (Unity)** để nướng (Bake) dữ liệu bản đồ. Đây là hệ thống cực kỳ tối ưu để tìm đường.
    - **Movement Logic:** 
        - Với quái thường (Swarm): **KHÔNG** dùng `NavMeshAgent`. Sử dụng logic di chuyển hướng về người chơi kết hợp **Steering Behaviors** đơn giản (né vật cản cơ bản).
        - Với Boss hoặc Quái đặc biệt: Sử dụng `NavMeshAgent` nếu cần vượt qua các địa hình phức tạp (mê cung).
    - **Performance Gap:** Nếu số lượng quái lên đến >500, sẽ chuyển sang sử dụng **Unity Jobs System** để tính toán hướng di chuyển song song trên nhiều nhân CPU.

---

## **16. DEBUG & CHEAT SYSTEM**
- **Cheat Console:** Toàn bộ logic quan trọng (Stats, Gold, XP) phải có hàm Debug đi kèm (ví dụ: `AddGoldDebug(int amount)`).
- **UI Toolkit Window:** Tạo một cửa sổ Debug ẩn (thường dùng 3 ngón tay chạm màn hình hoặc phím `~`) để gọi các hàm Cheat mà không cần compiler lại code.
- **Validation Wrapper:** Sử dụng `GameLog` tập trung để dễ dàng lọc lỗi (Error Filtering) và có thể mở rộng để gửi Log về server sau này.
