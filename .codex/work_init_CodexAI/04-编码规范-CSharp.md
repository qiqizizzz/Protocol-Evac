# Unity C# 开发规范

## 一、注释规范（必读）

# 文件头注释（强制要求）

- **每个 `.cs` 文件的最顶部**必须添加文件头注释块
- `描述` 填写该类的用途与职责简介
- `类名` 填写与文件名一致的类名（含 `.cs` 后缀）
- `创建` 固定署名 `By qiqizizzz`
- 格式严格保持如下，不得随意更改边框样式

```csharp
/*
* ┌──────────────────────────────────┐
* │  描    述: 玩家控制器，负责处理移动、跳跃与受击逻辑
* │  类    名: PlayerController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/
```

### 方法与类注释（强制要求）

- **每个方法义上方**戳情添加 `//` 单行注释，主要简要描述该类方法的职责与用途，但是如果这个类名非常简洁明了，或者类方法本身很简单，那么则不需要添加，比如`OnInit`，`UnInit`，`OnOpen`，`OnClose`等
- 注释结尾不要加句号
- **除了 Unity 生命周期外，自定义的每个方法定义上方**必须添加注释，方式二选一：
    - 方式1：`//` 单行注释 —— 用于简单方法
    - 方式2：XML 文档注释 `<summary>` —— 用于公共 API 或需要参数说明的方法
- **不要同时写** `//` 和 XML，二选一即可
- 复杂逻辑块内部使用行内注释说明意图，避免写显而易见的注释

```csharp
/*
* ┌──────────────────────────────────┐
* │  描    述: 玩家控制器，负责处理移动、跳跃与受击逻辑
* │  类    名: PlayerController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

// 玩家控制器，负责处理移动、跳跃和受击逻辑
public class PlayerController : MonoBehaviour
{
    private void Awake() { }
    private void Update() { }

    /// <summary>
    /// 对玩家造成伤害
    /// </summary>
    /// <param name="damage">伤害数值</param>
    /// <param name="damageType">伤害类型</param>
    public void TakeDamage(float damage, DamageType damageType)
    {
        if (m_isInvincible) return;
        float finalDamage = CalculateFinalDamage(damage, damageType);
        m_currentHealth -= finalDamage;
        OnHealthChanged?.Invoke(m_currentHealth);
    }
}
```

### 代码变更说明（强制要求）

每次修改代码后，必须在回复末尾附上变更摘要，格式如下：

```
📝 代码变更摘要
├─ 修改类：PlayerController
│   ├─ 新增方法：TakeDamage() —— 处理受击逻辑
│   └─ 修改方法：Update() —— 新增跳跃输入检测
└─ 新增类：BulletPool
    └─ 新增方法：GetBullet()、ReturnBullet() —— 对象池核心逻辑
```

---

## 二、命名规范

### 类与接口

| 类型             | 规则                     | 示例                              |
| ---------------- | ------------------------ | --------------------------------- |
| 普通类           | PascalCase               | `PlayerController`, `GameManager` |
| 接口             | `I` + PascalCase         | `IDamageable`, `IInteractable`    |
| 抽象类           | PascalCase + `Base` 后缀 | `CharacterBase`, `WeaponBase`     |
| ScriptableObject | PascalCase + `SO` 后缀   | `ItemDataSO`, `GameConfigSO`      |
| 枚举             | PascalCase               | `DamageType`, `GameState`         |

### 方法与属性

| 类型     | 规则                                  | 示例                                      |
| -------- | ------------------------------------- | ----------------------------------------- |
| 公共方法 | PascalCase                            | `TakeDamage()`, `MoveToPosition()`        |
| 私有方法 | camelCase                             | `calculateDamage()`, `updateUI()`         |
| 属性     | PascalCase                            | `public int Health { get; private set; }` |
| 事件     | `On` + PascalCase                     | `OnDamageTaken`, `OnItemCollected`        |
| 协程     | PascalCase + `Coroutine` 后缀（可选） | `SpawnEnemyCoroutine()`                   |

### 字段命名（个人规范）

| 类型                       | 规则                                | 示例                            |
| -------------------------- | ----------------------------------- | ------------------------------- |
| **纯私有字段**             | `m_` + camelCase                    | `m_animator`, `m_currentHealth` |
| **SerializeField private** | PascalCase                          | `MoveSpeed`, `JumpForce`        |
| **protected 字段**         | camelCase                           | `stats`, `config`, `animator`   |
| 常量                       | UPPER_SNAKE_CASE                    | `MAX_HEALTH`, `DEFAULT_SPEED`   |
| 静态只读                   | `S` + PascalCase                    | `S_DefaultSpawnPoint`           |
| 公共字段                   | 尽量避免，改用属性或 SerializeField | —                               |

```csharp
// ✅ 纯私有字段 —— m_ 前缀
private Rigidbody m_rigidbody;
private bool m_isGrounded;
private float m_currentHealth;

// ✅ SerializeField private
[SerializeField] private float MoveSpeed = 5f;
[SerializeField] private LayerMask GroundLayer;

// ❌ 错误：SerializeField 用了 m_ 前缀
[SerializeField] private float m_MoveSpeed;

// ❌ 错误：纯私有字段没有 m_ 前缀
private Rigidbody rigidbody;
```

### Unity 特定命名

- **Layer 变量**：`m_enemyLayer`（纯私有）
- **Tag 字符串常量**：`const string PLAYER_TAG = "Player";`
- **Scene 名称**：PascalCase，如 `MainMenu`, `Level01`
- **Animator 参数**：camelCase，如 `isWalking`, `attackTrigger`

---

## 三、代码组织

### 类内成员顺序

```csharp
public class PlayerController : MonoBehaviour
{
    // ==================== 常量与静态字段 ====================
    private const float MAX_HEALTH = 100f;
    private static readonly WaitForSeconds m_respawnDelay = new WaitForSeconds(3f);

    // ==================== 字段[外部设置] ====================
    [Header("移动设置")]
    [Tooltip("角色移动速度（单位/秒）")]
    [SerializeField, Range(0f, 20f)] private float MoveSpeed = 5f;

    // ==================== 属性 ====================
    public float Health { get; private set; }
    public bool IsGrounded { get; private set; }

    // ==================== 字段[私有] ====================
    private Rigidbody m_rigidbody;
    private Animator m_animator;

    // ==================== 事件 ====================
    public event Action<float> OnHealthChanged;
    public event Action OnPlayerDeath;

    // ==================== 生命周期 ====================
    private void Awake() { }
    private void OnEnable() { }
    private void Start() { }
    private void FixedUpdate() { }
    private void Update() { }
    private void LateUpdate() { }
    private void OnDisable() { }
    private void OnDestroy() { }

    // ==================== Public Function ====================
    public void TakeDamage(float damage) { }

    // ==================== Private Function ====================
    private void handleMovement() { }

    // ==================== Coroutine ====================
    private IEnumerator RespawnCoroutine() { yield return null; }

    // ==================== Gizmos ====================
    private void OnDrawGizmosSelected() { }
}
```

### 命名空间

- 格式：`公司名.项目名.模块名`，如 `MyStudio.RPGGame.Player`
- 按功能模块划分：`Core`, `Player`, `Enemy`, `UI`, `Audio`, `Utils`
- 每个文件只定义一个类，文件名与类名保持一致

---

## 四、Unity API 使用规范

### SerializeField 最佳实践

```csharp
[Header("战斗属性")]
[Tooltip("最大生命值")]
[SerializeField, Range(1f, 500f)] private float MaxHealth = 100f;

[Tooltip("攻击冷却时间（秒）")]
[SerializeField, Min(0f)] private float AttackCooldown = 0.5f;
```

### 组件获取

- **Awake()** 中获取并缓存所有组件引用到 `m_` 字段
- 优先使用 `TryGetComponent` 避免空引用异常
- 禁止在 `Update()` 等高频方法中调用 `GetComponent`

```csharp
private void Awake()
{
    m_rigidbody = GetComponent<Rigidbody>();
    if (!TryGetComponent<Animator>(out m_animator))
    {
#if UNITY_EDITOR
        Debug.LogError($"[{nameof(PlayerController)}] 未找到 Animator：{gameObject.name}");
#endif
    }
}
```

### 空检查

```csharp
if (m_target == null) return;       // ✅ 正确
if (m_target is null) return;       // ❌ 错误，无法检测已销毁对象
```

### 事件订阅与取消

```csharp
private void OnEnable()
{
    GameManager.Instance.OnGameOver += handleGameOver;
}

private void OnDisable()
{
    if (GameManager.Instance != null)
        GameManager.Instance.OnGameOver -= handleGameOver;
}
```

---

## 五、性能优化规范

### 缓存原则

```csharp
private Transform m_transform;

private void Awake() { m_transform = transform; }

private void Update()
{
    m_transform.position += m_inputDirection * (MoveSpeed * Time.deltaTime);
}
```

### 物理操作

```csharp
// 所有物理操作必须放在 FixedUpdate 中执行
private void FixedUpdate()
{
    m_rigidbody.MovePosition(m_rigidbody.position + m_velocity * Time.fixedDeltaTime);
}
```

### 对象池

```csharp
private void fire()
{
    GameObject bullet = BulletPool.Instance.GetBullet();
    bullet.transform.SetPositionAndRotation(m_firePoint.position, m_firePoint.rotation);
    bullet.SetActive(true);
}
```

### 字符串与 Tag 比较

```csharp
if (other.CompareTag("Enemy")) { }   // ✅ 正确
if (other.tag == "Enemy") { }        // ❌ 错误，产生 GC Alloc

private const string ENEMY_TAG = "Enemy";
if (other.CompareTag(ENEMY_TAG)) { }
```

### 协程优化

```csharp
private static readonly WaitForSeconds m_waitOneSecond = new WaitForSeconds(1f);

private IEnumerator CountdownCoroutine(int seconds)
{
    for (int i = seconds; i > 0; i--)
    {
        yield return m_waitOneSecond;
        OnCountdownTick?.Invoke(i);
    }
}
```

---

## 六、代码风格

### 括号与格式

```csharp
if (isReady)   // ✅ 正确，对于括号内只有一行代码,优先省略括号
    StartGame();

if (isReady)   // ✅ 正确，括号内多行使用大括号
{
    StartGame();
    StartAudio();
}
```

### 访问修饰符

- 所有成员**必须**显式声明访问修饰符
- 能用 `private` 就不用 `protected`，能用 `protected` 就不用 `public`

```csharp
[SerializeField] private float MoveSpeed;  // ✅ 正确
float MoveSpeed;                            // ❌ 缺少访问修饰符
```

### 其他风格约定

- 使用 `var` 仅当类型从右侧赋值可明显推断时
- 三元运算符只用于简单赋值，不嵌套使用
- 每个文件末尾保留一个空行

---

## 七、调试与日志

### 日志规范

- 统一使用 `Debug.Log` / `Debug.LogWarning` / `Debug.LogError`
- 所有日志必须包裹在 `#if UNITY_EDITOR` 中，**不允许**在 Release 构建中输出日志
- 日志格式统一为 `[类名] 描述`，方便 Console 过滤定位

```csharp
#if UNITY_EDITOR
Debug.Log($"[{nameof(PlayerController)}] 玩家已死亡");
Debug.LogWarning($"[{nameof(PlayerController)}] 血量低于阈值：{m_currentHealth}");
Debug.LogError($"[{nameof(PlayerController)}] 未找到 Animator：{gameObject.name}");
#endif
```

### 必要引用校验（Awake 中强制检查）

对于**必须存在**的引用（SerializeField 拖拽、Find 查找、资源加载），  
**不要用判空静默跳过**，要在 `#if UNITY_EDITOR` 中主动报错，让问题立刻暴露：

```csharp
private void Awake()
{
    Txt_Title  = transform.Find("Bg/Txt_Title")?.GetComponent<TextMeshProUGUI>();
    Btn_Battle = transform.Find("Bg/Btn_Battle")?.GetComponent<Button>();
    Btn_Shop   = transform.Find("Bg/Btn_Shop")?.GetComponent<Button>();

#if UNITY_EDITOR
    if (Txt_Title  == null) Debug.LogError($"[{nameof(YourClass)}] Txt_Title 未找到，请检查层级路径");
    if (Btn_Battle == null) Debug.LogError($"[{nameof(YourClass)}] Btn_Battle 未找到，请检查层级路径");
    if (Btn_Shop   == null) Debug.LogError($"[{nameof(YourClass)}] Btn_Shop 未找到，请检查层级路径");
#endif
}
```

> **关键区别**：`#if UNITY_EDITOR` 内的判空是**主动报错**，不是静默跳过。  
> Release 构建中该代码块完全不存在，零性能开销。

### Gizmos 调试绘制

```csharp
private void OnDrawGizmosSelected()
{
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, AttackRange);

    Gizmos.color = Color.red;
    Gizmos.DrawRay(transform.position, transform.forward * ViewDistance);
}
```

---

## 八、判空规范

### 核心原则

> **该报错的地方就让它报错，不要用判空掩盖配置缺失问题。**

判空的目的是**保护运行时逻辑**，而不是**隐藏开发期错误**。  
错误的判空会让 Bug 静默消失，导致调试时无从下手。

### ❌ 禁止：用判空掩盖「应当存在的引用」

#### 1. 组件引用初始化

```csharp
// ❌ 错误：判空后 Find，掩盖了「忘记拖拽 / 路径写错」的问题
if (Txt_Title == null)
    Txt_Title = transform.Find("Bg/Txt_Title")?.GetComponent<TextMeshProUGUI>();

// ✅ 正确：直接赋值，找不到就在编辑器下报错
Txt_Title = transform.Find("Bg/Txt_Title")?.GetComponent<TextMeshProUGUI>();
#if UNITY_EDITOR
if (Txt_Title == null) Debug.LogError($"[{nameof(YourClass)}] Txt_Title 未找到");
#endif
```

#### 2. 事件绑定

```csharp
// ❌ 错误：按钮没拖拽也不报错，功能静默失效
if (Btn_Battle != null)
    Btn_Battle.onClick.AddListener(onBattleClick);

// ✅ 正确：直接绑定，引用为空时立刻抛出异常暴露问题
Btn_Battle.onClick.AddListener(onBattleClick);
```

#### 3. 资源加载

```csharp
// ❌ 错误：加载失败直接 return，调用方完全不知道发生了什么
if (ResKit.LoadGameObj("Prefabs/UI/UI_AllCardPanel") == null)
    return;

// ✅ 正确：加载后校验，失败时明确报错
var prefab = ResKit.LoadGameObj("Prefabs/UI/UI_AllCardPanel");
#if UNITY_EDITOR
if (prefab == null) Debug.LogError("[YourClass] 资源加载失败：Prefabs/UI/UI_AllCardPanel");
#endif
```

### ✅ 允许：真正需要判空的场景

| 场景               | 说明                     | 示例                                |
| ------------------ | ------------------------ | ----------------------------------- |
| **可选引用**       | 该字段本身就是可有可无的 | 可选特效、可选音效组件              |
| **运行时动态对象** | 对象可能已被销毁         | `if (m_target == null) return;`     |
| **外部传入参数**   | 调用方可能传 null        | 公共 API 的参数校验                 |
| **单例访问**       | 场景切换时单例可能不存在 | `if (GameManager.Instance != null)` |

```csharp
// ✅ 可选特效，允许判空
if (m_hitEffect != null)
    m_hitEffect.Play();

// ✅ 运行时目标可能已销毁
if (m_target == null) return;

// ✅ 事件取消订阅，单例可能已销毁
private void OnDisable()
{
    if (GameManager.Instance != null)
        GameManager.Instance.OnGameOver -= handleGameOver;
}
```

---

## 九、常见错误速查

| ❌ 错误做法                         | ✅ 正确做法                                        |
| ---------------------------------- | ------------------------------------------------- |
| 构造函数中使用 Unity API           | 改用 `Awake()` 或 `Start()`                       |
| `OnDestroy` 中访问其他 GameObject  | 销毁顺序不确定，应提前解引用                      |
| `Update` 中调用 `FindObjectOfType` | 在 `Awake/Start` 中缓存引用                       |
| `is null` 检查 Unity 对象          | 使用 `== null`                                    |
| 协程中 `new WaitForSeconds()`      | 提前缓存为静态只读字段                            |
| `other.tag == "xxx"` 比较 Tag      | 使用 `other.CompareTag("xxx")`                    |
| 公共字段暴露给外部                 | 使用 `[SerializeField] private` + 属性            |
| 缺少访问修饰符                     | 所有成员显式声明                                  |
| 缺少文件头注释块                   | 每个 `.cs` 文件顶部必须添加标准文件头             |
| SerializeField 字段用 `m_` 前缀    | SerializeField 统一使用 PascalCase                |
| 纯私有字段无前缀                   | 纯私有字段统一使用 `m_` 前缀                      |
| 必要引用判空后静默跳过             | `#if UNITY_EDITOR` 内用 `Debug.LogError` 主动报错 |
| 日志散落在 Release 构建中          | 所有日志包裹在 `#if UNITY_EDITOR` 中              |