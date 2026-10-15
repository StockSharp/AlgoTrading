# XAUUSD トレンド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は EMA クロスオーバー、RSI 極値、Bollinger Bands を使用して XAUUSD を取引します。
速い EMA が遅い EMA を上抜け、RSI が売られすぎ水準を下回り、価格が Bollinger Bands の上限を上回って終値をつけたときにロングポジションを建てます。
ショートポジションは逆の条件で建てます。
リスク管理はポートフォリオのリスク率とテイクプロフィット対ストップロス比率に基づいてストップロスおよびテイクプロフィット水準を設定します。

## 詳細

- **エントリー**:
  - ロング: 速い EMA が遅い EMA を上抜け、RSI < oversold、close > 上限バンド。
  - ショート: 速い EMA が遅い EMA を下抜け、RSI > overbought、close < 下限バンド。
- **エグジット**: リスク設定から計算されたストップロスまたはテイクプロフィット。
- **インジケーター**: EMA、RSI、Bollinger Bands。
