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
