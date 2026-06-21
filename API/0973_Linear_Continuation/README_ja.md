# 線形継続戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この例では TradingView の「Linear Continuation」スタディを StockSharp に変換します。戦略は 3 本の移動平均線を計算し、その線形継続投影をログに記録します。インジケーターのショーケースを目的としており、取引ロジックは含まれていません。

## 詳細

- **MA Type** – 移動平均の種類（SMA または EMA）
- **MA1 Period** – 1 本目の移動平均の期間
- **MA2 Period** – 2 本目の移動平均の期間
- **MA3 Period** – 3 本目の移動平均の期間
- **Aggressive Mode** – 投影距離の計算を切り替え

## 注意事項

- 注文は送信されません。
- デモンストレーション目的で設計されています。
