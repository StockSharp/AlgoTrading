# MACD 与 KDJ 马丁格尔策略
[English](README.md) | [Русский](README_ru.md)

该策略在 MACD 线与 KDJ 的 %K 线同时穿越各自的信号线时入场，并使用马丁格尔方式加仓：当价格向不利方向移动到设定百分比并反弹后再次加仓。

当达到止盈、止损或触发跟踪止损时平仓。

## 细节

- **入场**：MACD 线与 %K 线同向穿越其信号线。
- **加仓**：价格逆势达到 `Add Position Percent` 并反弹 `Rebound Percent` 后，最多加仓 `Max Additions` 次，每次数量乘以 `Add Multiplier`。
- **出场**：达到 `Take Profit Trigger`、`Stop Loss Percent` 或触发跟踪止损时平仓。
- **方向**：多空皆可。

