# Ticks In MySQL Recorder 策略

## 概述
- 基于 `MQL/7850` 中的 MetaTrader 4 专家顾问 `TicksInMySQL.mq4` 改写。
- 连接 MySQL 数据库，在每个一级行情（tick）到达时插入一条记录。
- 不发送任何委托，仅保存组合的占用保证金、可用保证金、权益、Bid/Ask 价格以及标的代码。
- 额外提供表结构自动创建和写入失败时自动停止等保护选项。

策略完全是被动式的，用于复现原始 EA 输出的账户遥测数据，方便在 StockSharp 之外进行报表或风险分析。

## MySQL 前置条件
- 需要 [MySql.Data](https://www.nuget.org/packages/MySql.Data/) 提供程序。请在宿主项目（Designer、Backtester、Shell 或自定义程序）中引用该包后再编译策略。
- 指定的数据库用户必须拥有向目标表写入的权限。只有在用户具备 `CREATE TABLE` 权限时才应启用 `EnsureTableExists`。
- 运行策略的应用需要能够访问 MySQL 服务器，请确认防火墙或 VPN 已正确放行。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `Server` | `localhost` | MySQL 服务器主机名或 IP 地址。 |
| `Port` | `3306` | 连接使用的 TCP 端口。 |
| `Database` | `mt4` | 存放目标表的数据库。 |
| `User` | `user` | 用于认证的登录名。 |
| `Password` | `pwd` | 登录密码（以明文形式存储在策略设置中）。 |
| `TableName` | `ticks` | 接收数据的表名。 |
| `EnsureTableExists` | `false` | 启动时若表不存在则创建，需要 DDL 权限。 |
| `PricePrecision` | `4` | Bid/Ask 的保留小数位，等价于 MQL 中的 `NormalizeDouble`。 |
| `StopOnInsertError` | `false` | 开启后，一旦写入报错即停止策略。 |

## 表结构
当启用 `EnsureTableExists` 时会执行以下语句（表名会按参数替换）：

```sql
CREATE TABLE IF NOT EXISTS `ticks` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `margin` DECIMAL(19,6) NOT NULL,
    `freemargin` DECIMAL(19,6) NOT NULL,
    `date` DATETIME NOT NULL,
    `ask` DECIMAL(19,6) NOT NULL,
    `bid` DECIMAL(19,6) NOT NULL,
    `symbol` VARCHAR(64) NOT NULL,
    `equity` DECIMAL(19,6) NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB;
```

如需不同的精度，可以调整列类型；运行时的插入语句按照相同的列顺序赋值。

## 写入内容
每条有效行情将写入以下字段：

- `margin`：当前占用保证金，对应 `Portfolio.BlockedValue`。
- `freemargin`：`Portfolio.CurrentValue - Portfolio.BlockedValue`，等同于 MQL 的 `AccountFreeMargin()`。
- `date`：行情时间戳（优先使用服务器时间，否则采用 `CurrentTime`），统一转换为 UTC。
- `ask`：按 `PricePrecision` 小数位四舍五入的最好卖价。
- `bid`：按 `PricePrecision` 小数位四舍五入的最好买价。
- `symbol`：策略所服务标的的 `Security.Id`。
- `equity`：组合权益，优先 `Portfolio.CurrentValue`，若不可用则退回 `Portfolio.BeginValue`。

在同时得到 Bid 与 Ask 之前，策略不会写入任何行，以避免记录不完整的数据。

## 使用流程
1. 在宿主解决方案中引用 MySql.Data 包。
2. 在参数中配置好数据库凭据，并为策略分配证券和投资组合。
3. 可选地启用 `EnsureTableExists`，或按照上表手动创建目标表。
4. 启动策略：通过 `SubscribeLevel1()` 订阅一级行情，并在每个 tick 上插入一行数据。
5. 关注日志输出。若 `StopOnInsertError = true`，出现第一次写入异常后策略会立即停止，满足需要快速告警的场景。

## 运维提示
- 插入操作由互斥锁保护，保证与原始 EA 一样按顺序写入数据库。
- 密码由 StockSharp 配置系统保存；如需更高安全性，可在外部配置加密存储。
- 策略不含交易逻辑，可与使用同一组合的其他交易算法并行运行。
- 如果预计要保存大量数据，建议在 `date` 等字段上建立索引以提升查询性能。
