# 移動平均戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

選択した価格タイプの短期移動平均が長期移動平均を上抜けたときにロングポジションを建てます。短期平均が長期平均を再び下抜けたときにポジションを決済します。

## 詳細
- **エントリー条件:** 短期MAが長期MAを上抜け。
- **エグジット条件:** 短期MAが長期MAを下抜け。
- **インジケーター:** SMA, EMA, DEMA, TEMA, WMA, VWMA。
- **価格ソース:** Close, High, Open, Low, Typical, Center。
- **ストップ:** なし。
- **デフォルト値:**
  - `MaType` = EMA
  - `ShortLength` = 1
  - `LongLength` = 20
  - `PriceType` = Typical
  - `CandleType` = 1 minute
- **フィルター:**
  - カテゴリ: トレンドフォロー
  - 方向: ロングのみ
  - インジケーター: 移動平均
  - ストップ: なし
  - 複雑さ: シンプル
  - リスクレベル: 中
