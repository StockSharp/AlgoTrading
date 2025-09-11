# FON60DK
[English](README.md) | [Русский](README_ru.md)

该策略在 Tillson T3 突破 Optimized Trend Tracker (OTT) 上轨且 Williams %R 显示多头动能时开多仓。当 Tillson T3 跌破相反的 OTT 下轨且 Williams %R 进入超卖区时平仓。

## 详情

- **入场条件**：`T3 > OTT_up` 且 `Williams %R > -20`
- **出场条件**：`T3_SAT < OTT_dn_SAT` 且 `Williams %R < -70`
- **类型**：趋势跟随
- **指标**：Tillson T3、OTT、Williams %R
- **时间框架**：1 分钟（默认）
- **止损**：无
