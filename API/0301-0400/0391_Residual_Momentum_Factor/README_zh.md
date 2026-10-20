# Residual Momentum Factor

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

ResidualMomentumFile 提供由外部研究或数据源计算的残差动量，不用 OHLC 或基准收益率代替。每月将最新有效已发布评分降序排列，买入前 floor(n/Decile) 个，做空同数末尾证券。至少需要 Decile 个有效证券。

必须显式设置 Universe。价格与 Capital 采用 USD，每个价格单位对应一个交易单位。Capital 是固定预算，Leverage 控制总敞口。不添加止损或止盈。

仅处理时间戳一致的已完成蜡烛。统计按不同 UTC 日期计数，同一天的盘中收盘值会被后续值替换。每月在第一个满足条件的同步收盘信号上再平衡一次；数据不足时延迟。相同分数按 Security.Id 排序。

多头和空头各分配 Capital × Leverage 的一半，各篮子内等额分配。按信号收盘价换算数量，并按 VolumeStep 向零取整。根据实际成交仓位调整，等待未完成订单，遵守 MinVolume/MaxVolume，并在后续蜡烛继续大额调整。MinTradeUsd 过滤小额调整，但完整平仓不受此过滤限制。

## 参数

| 参数 | 默认值 |
|---|---|
| `Universe` | 必填 |
| `Capital` | 100000 |
| `Leverage` | 1 |
| `MinTradeUsd` | 200 |
| `CandleType` | TimeSpan.FromMinutes(5).TimeFrame() |
| `ResidualMomentumFile` | 必填 |
| `Decile` | 10 |
| `MaxScoreAgeDays` | 400 |

## 输入数据

必需评分 CSV：`SecurityId,PeriodEnd,PublishedAt,Score`。PeriodEnd 用 `yyyy-MM-dd`，PublishedAt 用 UTC `yyyy-MM-ddTHH:mm:ssZ`，Score 为十进制数。拒绝重复修订或期末前发布。按真实同步蜡烛 CloseTime 选择最新已发布期间及其最新修订。`MaxScoreAgeDays` 从 PeriodEnd 起算。缺失、过期或证券不足则延迟审查并保留仓位，价格不能代替评分。

## 验证

两种语言使用随附 BTC/TON，检查独立公式、订单方向/数量、真实成交、仓位与退出。带日期的价格、因子、日历数据验证机制，重定位 OHLC/成交量不是多年历史。两个排名组、TopN=1、SMA=2 是明确的小证券池测试配置，长窗口和原始默认值另行检查。不承诺盈利。
