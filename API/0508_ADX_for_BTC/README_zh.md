# ADX for BTC
[English](README.md) | [Русский](README_ru.md)

该策略使用平均趋向指数 (ADX) 并可选 SMA 趋势过滤器，以捕捉比特币中的强势行情。

回测显示平均年化收益约 80%。该策略在加密市场表现最佳。

当 ADX 上穿入场阈值且趋势过滤器看多时买入；当 ADX 下穿退出阈值时平仓。

## 细节

- **入场条件**：ADX 上穿 `EntryLevel`，且（若启用）快速 SMA > 慢速 SMA。
- **多空方向**：仅做多。
- **出场条件**：ADX 下穿 `ExitLevel`。
- **止损**：否。
- **默认值**：
  - `EntryLevel` = 14m
  - `ExitLevel` = 45m
  - `SmaFilter` = true
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类别：趋势
  - 方向：多头
  - 指标：ADX, SMA
  - 止损：否
  - 复杂度：基础
  - 时间框架：日内 (5m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
