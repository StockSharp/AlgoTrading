# MySQL 点价记录策略

## 概述
- 基于 StockSharp 平台重写 MetaTrader 4 的 “TicksInMySQL” 专家顾问。
- 连接 MySQL 数据库，并把每一笔一级行情与当前投资组合指标一起写入数据库。
- 仅记录保证金占用、可用保证金、权益、买卖报价以及交易品种标识，不会发送任何订单。

该策略适用于需要把逐笔账户遥测信息写入外部数据库以便风控或报表分析的场景。策略只负责写入数据，不会主动交易。

## MySQL 依赖
- 示例依赖 [MySql.Data](https://www.nuget.org/packages/MySql.Data/) 数据提供程序。请在加载策略的宿主应用（Backtester、Designer 或自定义项目）中添加该包。
- MySQL 用户需要拥有在目标库中创建表（若启用自动建表）以及插入数据的权限。
- 运行策略的主机必须能够访问 MySQL 服务器的网络端口。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `Server` | `localhost` | MySQL 服务器主机名或 IP 地址。 |
| `Port` | `3306` | 连接服务器所使用的 TCP 端口。 |
| `Database` | `mt4` | 存放目标数据表的数据库名称。 |
| `User` | `user` | 用于认证的登录名。 |
| `Password` | `pwd` | 登录密码（以明文方式保存在设置中）。 |
| `TableName` | `ticks` | 用于接收数据的表名。 |
| `AutoCreateTable` | `true` | 启动时若表不存在则自动创建，需要具备 DDL 权限。 |
| `PricePrecision` | `4` | 写入买卖价时保留的小数位数，对应 MQL 中的 `NormalizeDouble`。 |

## 数据表结构
当启用 `AutoCreateTable` 时，策略会执行如下建表语句（表名会根据参数替换）：

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

如需更高精度，可根据实际情况调整列类型。运行时执行的插入语句与上述列顺序保持一致。

## 写入字段
每次获取到完整的一级行情（同时具备买价和卖价）都会写入一行数据，包含：

- `margin`：当前 `Portfolio.BlockedValue` 值（订单或保证金占用）。
- `freemargin`：`Portfolio.CurrentValue - Portfolio.BlockedValue` 计算出的可用保证金。
- `date`：行情时间（优先使用服务器时间，否则使用策略当前时间），以 UTC 存储。
- `ask`：根据 `PricePrecision` 参数四舍五入后的最优卖价。
- `bid`：根据 `PricePrecision` 参数四舍五入后的最优买价。
- `symbol`：策略处理的品种 `Security.Id`。
- `equity`：投资组合权益（优先 `Portfolio.CurrentValue`，若不可用则退回 `BeginValue`）。

在买价或卖价尚未出现之前，不会写入数据以避免不完整的记录。

## 使用流程
1. 在宿主解决方案中添加 MySql.Data 包。
2. 在策略参数中配置有效的 MySQL 连接信息，并在启动前分配证券与投资组合。
3. （可选）启用 `AutoCreateTable` 以首次建表，或根据上述结构手动创建表。
4. 启动策略后会订阅一级行情，在获取到第一条完整报价后立即写入数据库。
5. 通过日志监控连接或写入错误（`LogError` 级别）。若启动时无法建立连接，策略会自动停止。

## 运维说明
- 通过内部锁保证数据库写入的串行执行，避免并发冲突。
- 策略不负责密码安全管理，建议在宿主应用中配置安全的凭据存储方案。
- 策略本身不含任何风控或交易逻辑，可与其他策略并行运行以执行交易。
- 若需要处理大量数据，建议在 `date` 等列上添加索引以优化查询性能。
