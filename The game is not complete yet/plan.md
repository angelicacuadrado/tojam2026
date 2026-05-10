# 敌人端实现计划

## 范围限制

本计划只处理敌人端，避免和玩家端开发产生合并冲突。

只改这些文件：

- `Assets/Scripts/Enemy/EnemyAttacker.cs`
- `Assets/Scripts/Enemy/EnemyHealth.cs`
- `Assets/Scripts/Enemy/EnemyGrower.cs`
- `Assets/Scripts/Enemy/EnemySpawner.cs`

不改这些文件：

- `Assets/Scripts/Player/PlayerHealth.cs`
- `Assets/Scripts/Player/PlayerAttack.cs`
- `Assets/Scripts/Player/PlayerController.cs`
- `Assets/Scripts/Bullet.cs`

敌人端需要调用玩家受伤逻辑时，只依赖 `PlayerHealth` 已经公开的稳定接口，不修改玩家端脚本的内部实现。

更新：`PlayerHealth.cs` 现在已经暴露玩家受伤接口：

```csharp
public void TakeDamage(int damage, Vector3 direction)
```

敌人攻击玩家时需要传入伤害和击退方向。`direction` 建议由敌人端计算，表示玩家应该被推开的方向；玩家端负责在 `PlayerHealth` 内部根据该方向对玩家 `Rigidbody` 施加力。

## 当前状态

- `Assets/Scripts/Enemy` 里面有四个空脚本：`EnemyAttacker.cs`、`EnemyGrower.cs`、`EnemyHealth.cs`、`EnemySpawner.cs`。
- 拉取玩家端代码后，`PlayerController.cs` 已经有移动、跳跃、视角控制实现。
- `PlayerHealth.cs` 已经有稳定的玩家受伤接口：`TakeDamage(int damage, Vector3 direction)`。
- `PlayerAttack.cs`、`Bullet.cs` 目前仍是空模板，没有稳定的玩家攻击敌人接口。
- `Level2` 目前看起来没有完整敌人对象、巡逻点、生成点和 NavMesh 数据，需要后续在 Unity Editor 里配置。
- 项目已经安装 `com.unity.ai.navigation`，敌人追踪可以使用 `NavMeshAgent`。

## 目标

敌人端需要实现：

- 敌人平时在 `pos1` 和 `pos2` 之间巡逻。
- 发现玩家后进入追踪/攻击模式。
- 可以通过开关控制敌人是否会主动追逐玩家。
- 使用 `NavMeshAgent` 追踪玩家。
- 进入攻击距离后攻击玩家。
- 每次攻击造成 `1` 点伤害。
- 攻击命中或碰撞玩家时，计算从敌人指向玩家的击退方向，并传给 `PlayerHealth.TakeDamage(...)`。
- 攻击有冷却时间。
- 由敌人生成脚本生成敌人，目前先生成一个。
- `EnemySpawner` 可以控制本次生成的敌人参数，例如是否追逐玩家、血量、伤害、攻击范围、成长倍率等。
- 敌人受到伤害后扣 HP。
- 敌人 HP 归零后由一个变量控制行为：
  - `Die`：敌人死亡。
  - `RespawnAndGrow`：敌人重生、恢复满 HP、scale 变大。

## 和玩家端的接口方案

为了避免修改玩家端脚本，敌人端只调用已经存在的稳定接口，不改 `PlayerHealth.cs` 的实现。

当前拉取后的玩家端状态：

- 可以依赖 `PlayerController` 已经存在，但敌人端不需要直接调用它。
- 可以依赖 `PlayerHealth.TakeDamage(int damage, Vector3 direction)` 作为敌人攻击玩家的接口。
- 暂时不能依赖玩家攻击脚本来测试敌人受伤，因为 `PlayerAttack.cs` 和 `Bullet.cs` 目前还没有实现伤害逻辑。

第一版建议在 `EnemyAttacker` 中直接获取玩家身上的 `PlayerHealth`，然后调用双参数接口：

```csharp
Vector3 knockbackDirection = (player.position - transform.position).normalized;
playerHealth.TakeDamage(damagePerAttack, knockbackDirection);
```

如果希望敌人控制击退力度，也可以把传入的 `Vector3` 做成带长度的力向量：

```csharp
Vector3 knockbackDirection = (player.position - transform.position).normalized;
Vector3 knockbackVector = knockbackDirection * knockbackForce;
playerHealth.TakeDamage(damagePerAttack, knockbackVector);
```

推荐第一版先使用单位方向向量，让 `PlayerHealth` 内部决定具体力的大小。这样敌人端只负责“从哪里推”，玩家端负责“怎么被推”。

不再推荐 `SendMessage`：

- `SendMessage` 只能干净传递单个参数，不适合 `TakeDamage(int, Vector3)` 这种双参数接口。
- 使用字符串方法名没有编译期检查，后续更容易出错。
- 现在 `PlayerHealth` 接口已经明确，直接调用更清晰。

## 脚本计划

### 1. EnemyAttacker.cs

职责：

- 管理敌人的巡逻、发现玩家、追踪、攻击。

需要字段：

- `Transform pos1`
- `Transform pos2`
- `Transform player`
- `bool canChasePlayer = true`
- `float detectionRange`
- `float attackRange`
- `float attackCooldown`
- `int damagePerAttack = 1`
- `float knockbackForce`，可选；如果击退力度完全由 `PlayerHealth` 控制，敌人端可以不使用该字段
- `float knockbackUpwardForce`，可选；用于让玩家被推开时略微向上弹起，第一版可以先设为 `0`
- `float patrolPointReachDistance`
- `NavMeshAgent agent`
- `PlayerHealth playerHealth`

内部状态：

- `Patrol`
- `Chase`
- `Attack`

巡逻逻辑：

- 默认状态是 `Patrol`。
- 使用 `NavMeshAgent.SetDestination(...)` 在 `pos1` 和 `pos2` 之间移动。
- 到达当前巡逻点后切换到另一个巡逻点。

发现玩家逻辑：

- 每帧检查敌人与 `player` 的距离。
- 如果 `canChasePlayer == true`，距离小于等于 `detectionRange` 时进入 `Chase`。
- 如果 `canChasePlayer == false`，忽略 `detectionRange`，敌人不会主动追逐玩家，继续巡逻。
- 如果没有配置 `player`，脚本不报错，只保持巡逻并输出一次 warning。

追踪逻辑：

- `Chase` 状态下持续调用 `agent.SetDestination(player.position)`。
- 玩家进入 `attackRange` 后进入 `Attack`。
- 玩家离开 `detectionRange` 后回到 `Patrol`。
- 如果运行时把 `canChasePlayer` 从 `true` 改成 `false`，敌人应该立刻停止追逐并回到 `Patrol`。

攻击逻辑：

- `Attack` 状态下停止或降低移动。
- 冷却结束后造成一次伤害。
- 攻击时计算击退方向：
  - 基础方向：`player.position - transform.position`。
  - 建议把 `y` 清零后 normalized，避免玩家被斜向下压进地面。
  - 如果需要向上弹开，再额外加 `Vector3.up * knockbackUpwardForce`。
- 伤害调用使用 `playerHealth.TakeDamage(damagePerAttack, knockbackDirectionOrVector)`。
- 如果玩家离开 `attackRange`，回到 `Chase`。

碰撞推动逻辑：

- 如果敌人 collider 和玩家 collider 会真实碰撞，使用 `OnCollisionEnter(Collision collision)` 或 `OnCollisionStay(Collision collision)` 检测玩家。
- `OnCollisionEnter` 适合“撞到的一瞬间攻击并击退”。
- `OnCollisionStay` 适合“持续贴住玩家时按冷却反复攻击并击退”。
- 碰撞检测到玩家后复用同一个 `TryAttackPlayer(...)` 方法，避免距离攻击和碰撞攻击写两套逻辑。
- `TryAttackPlayer(...)` 内部统一处理冷却、伤害、击退方向计算。
- `canChasePlayer` 只控制主动追逐，不控制碰撞攻击；即使敌人不会追玩家，玩家主动撞上敌人仍然会受伤和被击退。

注意：

- `EnemyAttacker` 可以引用 `PlayerHealth`，因为玩家受伤接口现在已经明确。
- `EnemyAttacker` 不修改玩家端任何文件。
- 如果玩家对象没有 `PlayerHealth`，敌人攻击不报错，只输出一次 warning。

### 2. EnemyHealth.cs

职责：

- 管理敌人 HP。
- 接收敌人受到攻击的伤害。
- 控制 HP 归零后的行为。

需要枚举：

```csharp
public enum EnemyZeroHPBehavior
{
    Die,
    RespawnAndGrow
}
```

需要字段：

- `int maxHP`
- `int currentHP`
- `EnemyZeroHPBehavior zeroHPBehavior`
- `float growthScaleMultiplier`
- `bool useDestroyOnDie`

需要方法：

- `public void TakeDamage(int amount)`
- `public void ResetHealth()`

HP 归零逻辑：

- 如果 `zeroHPBehavior == Die`：
  - `useDestroyOnDie == true` 时使用 `Destroy(gameObject)`。
  - `useDestroyOnDie == false` 时使用 `gameObject.SetActive(false)`。
- 如果 `zeroHPBehavior == RespawnAndGrow`：
  - 恢复 `currentHP = maxHP`。
  - 在死亡位置原地复活。
  - 调用 `EnemyGrower.Grow(growthScaleMultiplier)`。

注意：

- `TakeDamage` 是敌人端对外暴露的受伤接口。
- 玩家攻击端之后只要调用敌人对象上的 `TakeDamage(int)` 即可。
- 不需要修改 `PlayerAttack.cs` 或 `Bullet.cs`。

### 3. EnemyGrower.cs

职责：

- 只负责敌人 scale 变大。

需要字段：

- `float maxScaleMultiplier`
- `Vector3 initialScale`

需要方法：

- `public void Grow(float multiplier)`
- 可选：`public void ResetScale()`

逻辑：

- `Grow` 根据当前 scale 乘以 `multiplier`。
- 如果设置最大倍率，就用初始 scale 计算上限，避免无限变大。

注意：

- 不处理 HP。
- 不处理死亡。
- 不处理生成。

### 4. EnemySpawner.cs

职责：

- 负责生成敌人。
- 允许开发者在 Inspector 中配置多种敌人 prefab。
- 每种 prefab 可以配置生成数量。
- 敌人具体参数由各自 prefab 自己保存，`EnemySpawner` 不覆盖 HP、伤害、追逐、成长等参数。

```csharp
[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject enemyPrefab;
    public int count = 1;
}
```

第一版可以把 `EnemySpawnEntry` 放在 `EnemySpawner.cs` 顶部，避免新增额外文件。

需要字段：

- `Transform spawnPoint`
- `Transform player`
- `Transform pos1`
- `Transform pos2`
- `bool spawnOnStart = true`
- `List<EnemySpawnEntry> enemiesToSpawn`

生成逻辑：

- 如果 `spawnOnStart == true`，`Start()` 时生成敌人。
- 遍历 `enemiesToSpawn`。
- 每个 entry 根据 `count` 生成对应数量的敌人。
- 如果 entry 没有配置 `enemyPrefab`，跳过该 entry 并输出 warning。
- 如果没有配置 `spawnPoint`，使用 spawner 自己的位置。
- 如果同一个 entry 要生成多个敌人，第一版可以全部生成在 `spawnPoint` 附近；为了避免重叠，建议增加一个小的圆形偏移。
- 生成后尝试设置敌人的引用：
  - `EnemyAttacker.player`
  - `EnemyAttacker.pos1`
  - `EnemyAttacker.pos2`

需要公开方法：

```csharp
public void SpawnAll()
```

用途：

- 开发者在 Inspector 中决定本关生成哪些 prefab、各几个。
- prefab 本身决定敌人行为，例如会不会追玩家、HP、伤害、死亡后是否变大。

注意：

- 生成脚本只改敌人端文件。
- 不创建或修改玩家对象。
- `EnemySpawner` 不负责修改生成出来的敌人参数。
- 不同敌人类型通过不同 prefab 表达，例如 `ChaserEnemy.prefab`、`PassiveEnemy.prefab`、`GrowEnemy.prefab`。

## 下一步改动计划：追逐开关和简单 Spawner

### 目标

- 每个敌人可以独立决定是否会主动追逐玩家。
- 开发者通过 Inspector 决定生成哪些敌人 prefab、各生成几个。
- `EnemySpawner` 不覆盖 prefab 上的敌人参数。
- 不改场景文件，不改玩家脚本。

### 需要修改/新增文件

- 修改：`Assets/Scripts/Enemy/EnemyAttacker.cs`
- 修改：`Assets/Scripts/Enemy/EnemySpawner.cs`

### 具体实施顺序

1. 在 `EnemyAttacker` 中新增 `canChasePlayer`。
2. 修改 `EnemyAttacker.Update()`：
   - `canChasePlayer == true` 时维持现有 Chase/Attack 行为。
   - `canChasePlayer == false` 时不进入 Chase。
   - 碰撞攻击仍然保留。
3. 修改 `EnemySpawner`：
   - 删除单个 `enemyPrefab` / `spawnCount` 的模式。
   - 增加 `List<EnemySpawnEntry> enemiesToSpawn`。
   - 每个 entry 包含 `enemyPrefab` 和 `count`。
   - 生成后只注入通用引用：`player`、`pos1`、`pos2`。
4. 跑 `dotnet build` 验证 C# 编译。

### 推荐测试配置

先制作两个 prefab：

`ChaserEnemy`：

- `canChasePlayer = true`
- `maxHP = 3`
- `damagePerAttack = 1`
- `detectionRange = 6`
- `attackRange = 1.5`
- `zeroHPBehavior = Die`

`PassiveEnemy`：

- `canChasePlayer = false`
- `maxHP = 1`
- `damagePerAttack = 1`
- `detectionRange = 0`
- `attackRange = 1.5`
- `zeroHPBehavior = RespawnAndGrow`
- `growthScaleMultiplier = 1.5`

测试时：

1. 在 `EnemySpawner.enemiesToSpawn` 中添加 `ChaserEnemy`，`count = 1`。
2. 再添加 `PassiveEnemy`，`count = 2`。
3. Play Mode 后确认一共生成 3 个敌人。
4. 确认 `ChaserEnemy` 会追玩家。
5. 确认 `PassiveEnemy` 只巡逻，不主动追玩家。
6. 对 `PassiveEnemy` 主动撞上去，确认仍然会触发碰撞攻击。
7. 对 `PassiveEnemy` 调用 `TakeDamage(1)`，确认原地复活并变大。

## Level2 场景配置需求

这些配置可能需要在 Unity Editor 里手动完成，尽量避免直接改 `.unity` 文件造成场景合并冲突。

需要对象：

- `EnemySpawner`
- `EnemySpawnPoint`
- `EnemyPatrolPos1`
- `EnemyPatrolPos2`
- 敌人 prefab
- Player 对象引用

敌人 prefab 需要：

- `NavMeshAgent`
- `EnemyAttacker`
- `EnemyHealth`
- `EnemyGrower`
- Collider，用于被玩家攻击命中，也用于和玩家发生碰撞/触发检测
- 如果使用 `OnCollisionEnter` / `OnCollisionStay`，敌人 collider 不要勾选 `Is Trigger`；玩家身上已有 `Rigidbody`，可以收到碰撞回调
- 如果使用 trigger 范围攻击，则改用 `OnTriggerEnter` / `OnTriggerStay`，敌人攻击 collider 需要勾选 `Is Trigger`

NavMesh 需要：

- Level2 里有可行走地面。
- 在 Unity Editor 中生成 NavMesh。
- 敌人生成点、`pos1`、`pos2` 都在 NavMesh 上或靠近 NavMesh。

## Inspector 默认值

建议第一版参数：

- `maxHP = 3`
- `damagePerAttack = 1`
- `knockbackForce = 6`，如果 `PlayerHealth` 只需要单位方向，则敌人端暂时不用这个值
- `knockbackUpwardForce = 0`
- `detectionRange = 8`
- `attackRange = 1.5`
- `attackCooldown = 1.0`
- `patrolPointReachDistance = 0.3`
- `growthScaleMultiplier = 1.2`
- `EnemySpawnEntry.count = 1`
- `zeroHPBehavior = Die`

## 测试计划

敌人端完成后，在 Unity Play Mode 中测试：

1. 打开 `Level2`。
2. 确认 `EnemySpawner` 生成一个敌人。
3. 确认敌人在 `pos1` 和 `pos2` 之间巡逻。
4. 把玩家放入 `detectionRange`。
5. 确认敌人使用 NavMesh 追踪玩家。
6. 玩家进入 `attackRange` 后，确认敌人按冷却时间触发攻击。
7. 确认敌人调用 `PlayerHealth.TakeDamage(int damage, Vector3 direction)`，每次攻击扣 `1` HP。
8. 确认传入的 `direction` 从敌人指向玩家，玩家被推离敌人；如果方向反了，检查 `player.position - enemy.position` 的计算。
9. 让敌人和玩家发生碰撞，确认 `OnCollisionEnter` 或 `OnCollisionStay` 能按冷却触发伤害和击退。
10. 手动或通过玩家攻击调用 `EnemyHealth.TakeDamage(...)`。
11. 设置 `zeroHPBehavior = Die`，确认敌人死亡。
12. 设置 `zeroHPBehavior = RespawnAndGrow`，确认敌人恢复满 HP、重生并变大。

## 合并冲突规避

- 不改玩家目录下任何脚本。
- 不改 `Bullet.cs`。
- 可以依赖 `PlayerHealth` 类型，但只调用 `TakeDamage(int damage, Vector3 direction)` 这个公开接口。
- 不使用 `SendMessage("TakeDamage", ...)`，因为新接口有两个参数。
- 玩家端不需要为了敌人攻击再改方法名；如果之后 `direction` 的语义从“方向”改成“完整力向量”，只需要双方同步 Inspector 参数和文档。
- 尽量不直接改 `Level2.unity`，场景引用优先让负责场景的人在 Unity Editor 里配置。
- 因为 `PlayerController.cs` 已经有其他人实现，敌人端不要引用或修改玩家移动逻辑。

## 当前额外注意

- 我尝试查看 git 状态时，Git 报告 `D:/TOJam2026` 存在 dubious ownership，当前环境无法直接读取 `git status`。
- 如果后面需要我检查改动范围，可以先由仓库所有者执行：

```powershell
git config --global --add safe.directory D:/TOJam2026
```

## 2026-05-08 今日进度记录

### 已实现脚本

- `EnemyAttacker.cs`
  - 使用 `NavMeshAgent` 在 `pos1` 和 `pos2` 之间巡逻。
  - 支持 `canChasePlayer` 开关。
  - `canChasePlayer == true` 时，玩家进入 `detectionRange` 后追逐玩家。
  - 玩家进入 `attackRange` 后停止移动并按冷却攻击。
  - `canChasePlayer == false` 时，不主动追逐玩家，只巡逻。
  - 碰撞攻击不受 `canChasePlayer` 影响，玩家主动撞上敌人仍会触发伤害和击退。
  - 攻击玩家时调用 `PlayerHealth.TakeDamage(int damage, Vector3 direction)`。
  - 击退方向按“敌人指向玩家”的水平向量计算，并支持 `knockbackForce` 和 `knockbackUpwardForce`。
  - 巡逻点到达判断改为 `NavMeshAgent.remainingDistance` 加水平距离兜底，避免敌人卡在巡逻点附近不切换目标。

- `EnemyHealth.cs`
  - 实现敌人 HP、`TakeDamage(int amount)`、`ResetHealth()`。
  - `EnemyZeroHPBehavior` 支持三种行为：
    - `Die`
    - `Respawn`
    - `RespawnAndGrow`
  - `Respawn`：死亡位置原地复活，不变大。
  - `RespawnAndGrow`：死亡位置原地复活，并调用 `EnemyGrower.Grow(...)` 变大。
  - 新增 `respawnCount`：
    - `0`：不复活。
    - 正数：最多复活对应次数。
    - `-1`：无限复活。
  - 复活位置固定为敌人死亡时的位置，不再使用 `respawnPoint`。

- `EnemyGrower.cs`
  - 实现 `Grow(float multiplier)`。
  - 支持 `maxScaleMultiplier`，限制敌人相对初始 scale 的最大成长倍率。
  - 实现 `ResetScale()`。

- `EnemySpawner.cs`
  - 从单个 prefab/spawnCount 模式改为列表模式。
  - 新增 `EnemySpawnEntry`：

```csharp
[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject enemyPrefab;
    public int count = 1;
}
```

  - 开发者可以在 Inspector 里配置多个敌人 prefab，以及每种 prefab 的生成数量。
  - Spawner 不覆盖 prefab 上的敌人参数；HP、伤害、是否追逐、成长行为都由各 prefab 自己决定。
  - 生成后仍会给敌人注入通用引用：`player`、`pos1`、`pos2`。
  - 多个敌人生成时会在 `spawnPoint` 附近按圆形偏移分散，避免完全重叠。

- `EnemyDamageTester.cs`
  - 临时测试脚本改为新版 Input System 写法。
  - 按 `T` 调用绑定敌人的 `EnemyHealth.TakeDamage(1)`。

### 场景/配置检查结果

- `Level2` 中已经有 `NavMeshSurface`，并且存在 baked NavMesh 数据。
- `Player` 已经挂上 `PlayerHealth`。
- 之前场景里预放 Enemy 和 Spawner 同时存在的问题已识别；推荐只通过 `EnemySpawner` 生成敌人。
- `Enemy.prefab` 曾出现 `detectionRange == attackRange` 的配置，这会导致敌人刚发现玩家就进入攻击状态，看不到追逐过程。建议保持：
  - `detectionRange > attackRange`
  - 例如 `detectionRange = 6`，`attackRange = 1.5`

### 验证

- 多次运行：

```powershell
dotnet build "The game is not complete yet.sln" --no-restore
```

- 最新结果：构建成功，`0 Warning(s), 0 Error(s)`。

## 2026-05-09 今日进度记录

### Mixamo / Zombie Cartoon 动画接入

- 排查 `Zombie Cartoon_01.prefab` 的绑定警告来源：
  - 模型本体 `Zombie Cartoon.fbx` 是 Humanoid。
  - Mixamo 导出的几个动画 FBX 一开始是 Generic，和模型 Avatar 不一致，容易产生绑定/Avatar 警告。
  - 旧的 `Main Camera.controller` 原本是商店资源自带的相机/演示 controller，不适合作为最终敌人动画控制器长期使用。
- 新增 Editor 菜单脚本：
  - `Assets/Editor/ZombieCartoonMixamoSetup.cs`
  - 菜单路径：`Tools/Zombie Cartoon/Setup Mixamo Animations`
  - 功能：
    - 将 `Zombie Cartoon.fbx` 设置为 Humanoid，并从本模型创建 Avatar。
    - 将 `Zombie Cartoon@Zombie Idle.fbx`、`Zombie Cartoon@Walking.fbx`、`Zombie Cartoon@Zombie Punching.fbx`、`Zombie Cartoon@Zombie Reaction Hit.fbx`、`Zombie Cartoon@Dying.fbx` 设置为 Humanoid，并 Copy From `Zombie Cartoon.fbx` 的 Avatar。
    - 创建/更新 `Zombie Cartoon.controller`。
    - 生成 `Idle`、`Walking`、`Punching`、`Reaction Hit`、`Dying` 状态。
    - 将新 controller 挂到 `Zombie Cartoon_01.prefab` 的 Animator 上。
- 动画循环设置约定：
  - `Idle` 和 `Walking` 需要 Loop。
  - `Punching`、`Reaction Hit`、`Dying` 不需要 Loop。

### Root Motion 约定

- 明确敌人移动和动画位移的职责：
  - 追踪、巡逻、普通走路由 `NavMeshAgent` / 敌人移动逻辑控制位置。
  - `Walking` 不使用 Root Motion，避免动画和 NavMeshAgent 抢 Transform。
  - `Dying` 可以使用 Root Motion，让死亡倒地动画控制最后的位移。
- `EnemyHealth.Die()` 已改为在触发死亡动画前打开 Root Motion：

```csharp
private void Die()
{
    if (animator == null)
        return;

    animator.applyRootMotion = true;
    animator.SetTrigger("die");
}
```

### 动画测试脚本

- 扩展 `EnemyDamageTester.cs`，用于 Play Mode 中快速测试敌人动画。
- 当前测试按键：
  - `T`：调用 `EnemyHealth.TakeDamage(1)`。
  - `I`：播放 `Idle`。
  - `W`：播放 `Walking`，并关闭 Root Motion。
  - `P`：播放 `Punching`。
  - `H`：播放 `Reaction Hit`。
  - `D`：播放 `Dying`，并打开 Root Motion。
- `EnemyDamageTester` 会自动查找子物体上的 `Animator`。
- 新增 `Control Root Motion For Testing` 开关，便于测试时决定是否由脚本自动切换 Root Motion。

### Skeleton 动画控制器理解和接入

- 排查 `Assets/Art/SazenGames/Skeleton/Art/Demo Animator Controllers`：
  - `idle.controller` 是基础 Animator Controller。
  - 其他 `.overrideController` 是 Animator Override Controller，用来复用基础 controller 的状态机，只替换动画 clip。
- Skeleton 的基础 controller 参数包括：
  - `attack1`
  - `attack2`
  - `hit`
  - `die`
  - `rebrith`
- Zombie controller 参数包括：
  - `attack`
  - `hit`
  - `die`
- 当前攻击触发约定：
  - Zombie 使用 `attack`。
  - Skeleton 随机使用 `attack1` 或 `attack2`。
- 注意：不建议长期用 `animator.name` 判断敌人类型，因为子物体改名后 trigger 会失效。后续建议改成 Inspector 配置攻击 trigger，或新增敌人动画配置 enum。

### Animation Event 死亡收尾

- 目标：死亡动画播放完以后再 Destroy 或 SetActive(false)。
- 已在 `EnemyHealth.cs` 中新增：

```csharp
public void OnDeathAnimationComplete()
{
    if (useDestroyOnDie)
        Destroy(gameObject);

    gameObject.SetActive(false);
}
```

- 已确认 FBX 只读动画仍然可以在 Unity Import Settings 的 `Animation > Events` 中添加事件，事件会写入 `.fbx.meta`，不是修改 FBX 本体。
- Skeleton 死亡动画：
  - `Skeleton_death.fbx` 已有 `OnDeathAnimationComplete` event。
  - event 时间为 `1`，表示 clip 末尾，设置方向正确。
- Zombie 死亡动画：
  - `Zombie Cartoon@Dying.fbx` 当前存在一个多余的 `NewEvent`，需要删除。
  - `OnDeathAnimationComplete` 需要放在时间 `1`，不能放在 `0`。
- 重要注意：
  - Animation Event 默认调用 Animator 所在 GameObject 上的方法。
  - 当前 `EnemyHealth` 在敌人根物体，Animator 在 `Zombie Cartoon_01` / `Skeleton_110` 子物体。
  - 后续应在模型子物体上挂一个 relay 脚本，将 `OnDeathAnimationComplete` 转发给父物体的 `EnemyHealth`。

建议 relay 脚本：

```csharp
using UnityEngine;

public class EnemyAnimationEventRelay : MonoBehaviour
{
    public void OnDeathAnimationComplete()
    {
        GetComponentInParent<EnemyHealth>()?.OnDeathAnimationComplete();
    }
}
```

### 当前遗留问题 / 下一步

- `EnemyAttacker.cs` 中 Skeleton 随机攻击建议改为 `UnityEngine.Random.value < 0.5f`，避免使用 `switch case float n when` 带来的 C# 版本兼容风险。
- `EnemyHealth.OnDeathAnimationComplete()` 建议改成 `if/else`，避免 `Destroy(gameObject)` 后又继续执行 `gameObject.SetActive(false)`。
- 需要补 `EnemyAnimationEventRelay`，并挂到 Animator 所在子物体上。
- Zombie 的 `Dying` Animation Event 需要手动清理：
  - 删除 `NewEvent`。
  - 把 `OnDeathAnimationComplete` 放到时间 `1`。
  - 点 `Apply`。

## 后续功能计划：压感按钮系统

### 目标

实现两种可复用的压感按钮 prefab / 脚本：

- 一次性按钮：
  - 被按下后保持按下状态。
  - 不会回弹。
  - 只触发一次按下事件。
- 重物压感按钮：
  - 需要有重物停留在按钮上才保持按下状态。
  - 重物离开后按钮回弹。
  - 按下时事件有效，回弹后事件失效。

两种按钮都需要支持在 Inspector 中拖拽配置事件，避免在按钮脚本里写死具体机关逻辑。

### 通用行为

- 按钮需要有清晰的状态：
  - `Released`
  - `Pressed`
- 从 `Released` 进入 `Pressed` 时触发 `OnPressed`。
- 从 `Pressed` 回到 `Released` 时触发 `OnReleased`。
- 机关逻辑应通过 `UnityEvent` 暴露：

```csharp
[SerializeField] private UnityEvent onPressed;
[SerializeField] private UnityEvent onReleased;
```

- 以后在 Inspector 里可以直接把门、平台、灯、陷阱、音效等对象的方法拖进事件列表。
- 按钮本身只负责检测压力和广播事件，不直接知道门或机关怎么工作。

### 一次性按钮

- 建议脚本名：`OneShotPressureButton`。
- 第一次被有效物体压下时：
  - 设置为永久 `Pressed`。
  - 触发 `onPressed`。
  - 禁止之后回弹。
- 离开按钮后：
  - 不触发 `onReleased`。
  - 仍保持按下状态。
- 适合用途：
  - 永久开门。
  - 一次性机关。
  - 剧情触发点。

### 重物压感按钮

- 建议脚本名：`WeightedPressureButton`。
- 只有符合条件的物体停留在触发区域内，按钮才保持 `Pressed`。
- 所有有效重物离开后：
  - 按钮回到 `Released`。
  - 触发 `onReleased`。
- 后续机关应利用 `onPressed` / `onReleased` 成对控制：
  - `onPressed`：开门、启用平台、通电。
  - `onReleased`：关门、停用平台、断电。
- 需要支持多个重物同时压在上面：
  - 只要有效物体数量大于 0，就保持按下。
  - 只有有效物体数量回到 0，才回弹。

### 有效压力来源

后续实现时建议至少支持一种筛选方式：

- 通过 LayerMask 判断哪些物体能压按钮。
- 或通过 Tag 判断，例如 `HeavyObject`。
- 更推荐 LayerMask，因为 Inspector 配置更直观，也不容易和其他 tag 冲突。

建议字段：

```csharp
[SerializeField] private LayerMask pressableLayers;
```

重物 prefab 需要：

- Collider。
- Rigidbody，或至少能稳定触发按钮的 trigger/collision 检测。
- 所在 Layer 包含在按钮的 `pressableLayers` 中。

### 物理检测建议

- 按钮上方放一个 Trigger Collider 作为压力检测区。
- 按钮脚本使用：
  - `OnTriggerEnter`
  - `OnTriggerExit`
- 对 Weighted 按钮，需要维护当前压在按钮上的有效 Collider 集合，避免多个 collider 或多个物体导致状态错乱。
- 对 OneShot 按钮，只要第一次检测到有效物体进入，就锁定为 Pressed。

### 视觉和音效

按钮状态切换时应预留表现接口：

- 按下时模型向下移动或切换材质。
- 回弹时模型恢复位置。
- 按下播放音效。
- 回弹播放音效。

这些表现可以先写在按钮脚本里，也可以后续拆成单独组件。第一版优先保证逻辑和 `UnityEvent` 可配置。

### 测试计划

一次性按钮测试：

1. 玩家或重物进入按钮检测区。
2. 确认按钮触发 `onPressed`。
3. 移开物体。
4. 确认按钮不回弹，也不触发 `onReleased`。
5. 再次进入按钮，确认不会重复触发 `onPressed`。

重物压感按钮测试：

1. 没有重物时按钮处于 Released。
2. 放一个有效重物到按钮上，确认触发 `onPressed`。
3. 重物停留时按钮保持 Pressed。
4. 移走重物，确认触发 `onReleased`。
5. 同时放两个重物，移走一个后按钮仍保持 Pressed。
6. 两个重物都移走后按钮才 Released。

### 当前暂不实现

- 不在按钮脚本里写死开门、关门、平台移动等逻辑。
- 不直接修改现有场景。
- 不先做复杂动画系统，第一版以状态、检测、UnityEvent 为主。

## 后续功能计划：门事件组件

### 目标

新增一个可被按钮 `UnityEvent` 调用的门控制脚本，用于：

- 开门：播放开门动画。
- 关门：倒放同一个开门动画。

按钮脚本不直接知道门的存在。场景配置时只需要在按钮的事件列表里拖入门对象，然后选择门脚本的公开方法。

### 建议脚本

建议脚本名：

- `DoorAnimationController`

建议放置路径：

- `Assets/Scripts/Mechanisms/DoorAnimationController.cs`

### 公开事件方法

脚本需要暴露两个 public 方法，方便在 Inspector 的 `UnityEvent` 中选择：

```csharp
public void OpenDoor()
public void CloseDoor()
```

配置方式：

- 一次性按钮：
  - `OnPressed -> DoorAnimationController.OpenDoor()`
  - 不需要配置关门。
- 重物压感按钮：
  - `OnPressed -> DoorAnimationController.OpenDoor()`
  - `OnReleased -> DoorAnimationController.CloseDoor()`

### 动画播放方式

第一版建议使用 `Animator` 控制门动画：

- 门对象上挂 `Animator`。
- Animator Controller 中至少有一个开门动画 state。
- `OpenDoor()`：
  - 将动画播放速度设为正数。
  - 从当前位置或从头播放开门动画。
- `CloseDoor()`：
  - 将动画播放速度设为负数。
  - 从当前位置或从末尾倒放开门动画。

建议字段：

```csharp
[SerializeField] private Animator animator;
[SerializeField] private string openStateName = "Open";
[SerializeField] private float playbackSpeed = 1f;
```

### 状态约束

门脚本需要避免重复触发造成状态混乱：

- 如果门已经开着，再调用 `OpenDoor()` 不应重播或抖动。
- 如果门已经关着，再调用 `CloseDoor()` 不应重播或抖动。
- 如果正在开门时调用关门，应允许从当前动画进度倒放。
- 如果正在关门时调用开门，应允许从当前动画进度正放。

建议维护状态：

```csharp
private bool isOpen;
private bool isMoving;
```

但第一版可以更简单：

- 通过 `Animator.Play(openStateName, 0, normalizedTime)` 控制播放方向。
- 开门时如果当前 normalized time 接近 1，视为已经打开。
- 关门时如果当前 normalized time 接近 0，视为已经关闭。

### 倒放注意事项

Unity 动画倒放时需要注意：

- Animator state 的 speed 可以设为负数，或使用 Animator 参数控制 speed multiplier。
- 如果直接设置 `animator.speed = -1`，会影响整个 Animator。
- 更稳的第一版可以只让门 Animator 里有一个动画 state，并由脚本设置 `animator.speed`。
- 关门前需要确保动画采样点在末尾或当前开门进度，否则从 0 倒放会看起来没反应。

建议逻辑：

```csharp
OpenDoor():
    animator.speed = playbackSpeed;
    animator.Play(openStateName, 0, currentNormalizedTime);

CloseDoor():
    animator.speed = -playbackSpeed;
    animator.Play(openStateName, 0, currentNormalizedTime);
```

如果当前动画没有有效进度：

- 开门默认从 `0` 开始。
- 关门默认从 `1` 开始。

### 配置要求

门 prefab / 场景物体需要：

- Animator。
- Animator Controller。
- 一个开门动画 state，名称和 `openStateName` 一致，默认建议叫 `Open`。
- 该开门动画不要勾 Loop。

按钮配置：

- 一次性按钮只拖 `OpenDoor()`。
- 重物按钮拖：
  - `OnPressed -> OpenDoor()`
  - `OnReleased -> CloseDoor()`

### 测试计划

一次性按钮 + 门：

1. 按钮被触发。
2. 确认 Console 不再依赖 Debug Log。
3. 确认门播放开门动画。
4. 移开物体后按钮不回弹，门保持打开。

重物按钮 + 门：

1. 放重物到按钮上。
2. 确认门正向播放开门动画。
3. 在门开到一半时移走重物。
4. 确认门从当前进度倒放关门。
5. 再次放上重物。
6. 确认门从当前进度继续正向打开。

### 当前暂不实现

- 不在按钮脚本里写死门逻辑。
- 不修改场景。
- 不强制创建门 prefab。
- 不做复杂门锁、钥匙条件、多个按钮组合逻辑；这些可以后续通过额外组件或事件组合实现。

## 2026-05-09 追加进度：按钮与 Exit 开关门

### 压感按钮实现

- 新增按钮脚本目录：
  - `Assets/Scripts/PressureButtons`
- 新增通用基类：
  - `PressureButtonBase.cs`
  - 负责：
    - `Pressed` / `Released` 状态切换。
    - `Pressable Layers` 过滤。
    - `OnPressed` 事件触发。
    - 可选按钮视觉下压位移。
  - 使用 `[RequireComponent(typeof(Collider))]`，添加组件时要求按钮物体带 Collider。
- 新增一次性按钮：
  - `OneShotPressureButton.cs`
  - 第一次被有效物体压下后永久保持 Pressed。
  - 只暴露 `OnPressed`，不暴露 `OnReleased`。
- 新增重物压感按钮：
  - `WeightedPressureButton.cs`
  - 有有效物体停留时 Pressed。
  - 所有有效物体离开后 Released。
  - 暴露 `OnPressed` 和 `OnReleased`。
  - 内部用 `HashSet<Collider>` 记录当前压在按钮上的有效 Collider，支持多个物体同时压住。
- 曾临时加入 Debug Log 以验证按钮事件触发：
  - pressed 时打印。
  - released 时打印。
  - 后续已按需求移除 Debug Log，按钮现在只触发 UnityEvent。

### 按钮场景配置排查

- 排查 `GrowEnemy` 为什么不能触发按钮：
  - `GrowEnemy` 有 `CapsuleCollider`。
  - `GrowEnemy` 有 `NavMeshAgent`，但没有 `Rigidbody`。
  - `NavMeshAgent` 不等于 `Rigidbody`。
  - Unity Trigger 事件通常要求参与双方至少有一个 Rigidbody。
- 结论：
  - 如果希望 `GrowEnemy` 能压按钮，需要给 `GrowEnemy` 或按钮一方补 Rigidbody。
  - 对 `GrowEnemy` 建议使用 kinematic Rigidbody，并关闭 gravity，避免影响 NavMeshAgent 移动。
  - 同时确认按钮的 `Pressable Layers` 包含 `GrowEnemy` 所在 Layer。

### Exit 合并开关门事件

- 原计划新增独立 `DoorAnimationController`。
- 后续发现项目已有 `Exit.cs`，因此门开关逻辑合并进 `Exit.cs`。
- 已删除不再需要的 `DoorAnimationController.cs`，并清理 `.csproj` 引用。
- `Exit.cs` 新增可被按钮 UnityEvent 调用的方法：

```csharp
public void OpenDoor()
public void CloseDoor()
```

- 配置方式：
  - 一次性按钮：`OnPressed -> Exit.OpenDoor()`
  - 重物按钮：
    - `OnPressed -> Exit.OpenDoor()`
    - `OnReleased -> Exit.CloseDoor()`
- `OpenDoor()` 行为：
  - 设置 Exit 为 open。
  - Exit Collider 变为 Trigger，允许玩家进入后完成关卡。
  - 指示灯切换为 open 材质。
  - 门动画正向播放到打开状态。
- `CloseDoor()` 行为：
  - 设置 Exit 为 closed。
  - Exit Collider 变为非 Trigger。
  - 指示灯切换为 closed 材质。
  - 门动画从当前进度倒放回关闭状态。

### 指示灯材质调整

- 原本 `Exit.cs` 会切换整个 Exit/Visuals 的 Renderer 材质。
- 已按需求改为只切换指示灯 Renderer：

```csharp
[SerializeField] private Renderer indicatorRenderer;
```

- `Exit` prefab / 场景中需要把指示灯子物体的 Renderer 拖到 `Indicator Renderer`。
- 不再自动抓取第一个子 Renderer，避免误改 `Visuals` 或门主体材质。

### Exit 动画排查

- 检查 `Assets/Art/Anmi/OpenDoor.anim`：
  - 动画路径为 `Gate_Small/Door`。
  - `Door.localPosition.z` 从 `0` 动到 `1.5`。
  - 这个路径和目标子物体设计一致。
- 检查 `Assets/Art/Anmi/Exit.controller`：
  - Animator state 名为 `OpenDoor`。
- 检查 `Exit.cs` 默认字段：
  - `openStateName` 默认值为 `Open`。
- 发现不播放动画的原因：
  - `Exit.OpenDoor()` 已触发，所以灯会变色。
  - 但脚本调用的是 `doorAnimator.Play("Open", ...)`。
  - Animator Controller 里没有叫 `Open` 的 state，实际 state 叫 `OpenDoor`。
- 解决方式：
  - 在 `Exit` 组件 Inspector 中把 `Open State Name` 从 `Open` 改成 `OpenDoor`。
  - 或后续把脚本默认值改成 `OpenDoor`。
- 额外注意：
  - `OpenDoor.anim` 当前 `Loop Time` 为 true。
  - 门动画不应循环，建议在动画 Inspector 中取消 `Loop Time`。

### 验证

- 多次运行：

```powershell
dotnet build "The game is not complete yet.sln" --no-restore
```

- 构建通过。
- 当前仍存在既有 warning：
  - `EnemyGrower.state` 字段已赋值但未使用。

## 2026-05-10 计划：玩家 HP 心心 GUI

### 目标

- 在游戏 HUD 上显示玩家 HP。
- 玩家最大 HP 默认为 3，对应 3 个心心图片。
- 每损失 1 点 HP，就隐藏 1 个心心。
- 复活或恢复满血时，重新显示全部心心。

### 参考现状

- 参考脚本：`Assets/Scripts/Player/PlayerHealth.cs`
- 当前 `PlayerHealth` 行为：
  - `maxHealth` 默认为 3。
  - `Start()` 中把 `currentHealth` 设置为 `maxHealth`。
  - `TakeDamage(int damage, Vector3 direction)` 中扣除 `currentHealth`。
  - `Respawn()` 中把 `currentHealth` 重置为 `maxHealth`。
- 当前问题：
  - `currentHealth` 和 `maxHealth` 是 private 字段，UI 不能直接安全读取。
  - 血量变化时没有事件或回调通知 UI。
  - 如果 UI 用 `Update()` 每帧轮询也能做，但不够干净，后续维护不方便。

### 实现方案

- 新增玩家血量 UI 脚本：
  - 建议路径：`Assets/Scripts/Player/PlayerHealthUI.cs`
  - 使用 `UnityEngine.UI.Image[] hearts` 保存 3 个心心图片。
  - `hearts[0]` 对应第 1 点 HP，`hearts[1]` 对应第 2 点 HP，`hearts[2]` 对应第 3 点 HP。
  - 刷新规则：
    - `i < currentHealth` 时显示心心。
    - `i >= currentHealth` 时隐藏心心。
- 修改 `PlayerHealth.cs`：
  - 暴露只读属性：

```csharp
public int CurrentHealth => currentHealth;
public int MaxHealth => maxHealth;
```

  - 新增血量变化事件：

```csharp
public event System.Action<int, int> HealthChanged;
```

  - 在以下位置触发 UI 刷新：
    - `Start()` 初始化血量后触发一次。
    - `TakeDamage()` 扣血后触发一次。
    - `Respawn()` 重置血量后触发一次。
- `PlayerHealthUI.cs` 负责：
  - 通过 Inspector 绑定 `PlayerHealth`。
  - 在 `OnEnable()` 订阅 `HealthChanged`。
  - 在 `OnDisable()` 取消订阅，避免对象销毁后残留回调。
  - 启用时立即读取当前血量刷新一次，避免 UI 初始状态为空。

### 场景配置

- 在 Level 场景中新增或复用一个 Canvas 作为游戏 HUD。
- 在 Canvas 左上角创建一个容器，例如 `HealthBar`。
- 在 `HealthBar` 下放 3 个 `Image` 子物体：
  - `Heart_1`
  - `Heart_2`
  - `Heart_3`
- 给 3 个 `Image` 使用同一张心心 Sprite。
- 把 3 个 `Image` 按顺序拖到 `PlayerHealthUI.hearts` 数组。
- 把玩家物体上的 `PlayerHealth` 拖到 `PlayerHealthUI.playerHealth`。

### 注意事项

- 如果后续 `maxHealth` 不是 3，`PlayerHealthUI` 可以继续工作：
  - `hearts` 数组长度决定最多显示多少颗心。
  - 当前计划先按 3 颗心配置。
- 隐藏心心建议使用 `heart.enabled = false`，这样 UI 布局位置保持不变。
- `TakeDamage()` 当前在死亡时不会设置无敌时间，短时间连续受击可能继续扣血；这不是本次 GUI 的重点，但测试时需要注意。
- `TakeDamage()` 中 `rb.AddForce(...)` 没有判空保护；这也不是本次 GUI 的重点，但如果玩家没有 Rigidbody，会影响受伤流程。

### 验证计划

- 进入关卡后确认默认显示 3 个心心。
- 让敌人攻击玩家 1 次，确认隐藏 1 个心心，剩 2 个。
- 再受击到 1 HP，确认只剩 1 个心心。
- HP 归零后等待复活，确认 3 个心心重新显示。
- 运行：

```powershell
dotnet build "The game is not complete yet.sln" --no-restore
```

- 确认 C# 编译通过。
