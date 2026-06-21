# Bollinger Bands 強化戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

市場が200期間EMAを上回っている間に価格がBollinger Bands下限を下回ったときに買いを入れます。  
ストップロスは `エントリー - ATR * stop` に設定され、価格がエントリーより `ATR * trail` 上昇した後、中央バンドがトレーリングターゲットになります。

## 詳細

- **エントリー条件**: `Low > EMA` かつ `Low <= 下限バンド`。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: トレーリング起動後に中央バンドを下回って終値が引けるか、ストップを下回る安値。
- **ストップ**: ATRベースのストップロス。
- **デフォルト値**:
  - Bollinger期間 = 20
  - EMA期間 = 200
  - ATR期間 = 14
  - Stop ATR = 1.75
  - Trail ATR = 2.25

