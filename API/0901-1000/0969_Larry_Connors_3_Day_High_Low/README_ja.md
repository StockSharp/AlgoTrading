# Larry Connors 3日間高値安値戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Larry Connors の 3 日間高値/安値平均回帰アプローチを実装します。

## ロジック

- 以下の条件で買い：
  - 終値が長期移動平均線を上回る。
  - 終値が短期移動平均線を下回る。
  - 高値と安値が 3 本連続で切り下がっている。
- 価格が短期移動平均線を上回って終値をつけたら決済。

## パラメーター

- **Long MA Length** — 長期 SMA の期間（デフォルト 200）
- **Short MA Length** — 短期 SMA の期間（デフォルト 5）
- **Candle Type** — 分析に使用する時間軸
