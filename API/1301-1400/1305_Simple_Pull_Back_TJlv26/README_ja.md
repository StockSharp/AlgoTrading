# シンプル プルバック戦略 TJlv26
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、価格が長期 SMA より上で短期 SMA より下にあり、RSI(3) が 30 未満で指定した日付範囲内にあるときに買います。パーセントベースのストップロスとテイクプロフィット、または価格が短期 SMA を上回り、前のローソク足の安値を下回ったときにイグジットします。

## 詳細

- **エントリー条件**:
  - **ロング**: 終値 > 長期 SMA、終値 < 短期 SMA、RSI(3) < 30、StartDate から EndDate の間。
- **エグジット条件**:
  - ストップロス: 価格 ≤ エントリー価格 × (1 − StopLossPercent/100)。
  - テイクプロフィット: 価格 ≥ エントリー価格 × (1 + TakeProfitPercent/100)。
  - 価格 > 短期 SMA かつ価格 < 前のローソク足の安値の場合はクローズ。
- **インジケーター**: SMA、RSI。
- **ストップ**: あり。
- **方向**: ロングのみ。
