# Hoffman Heiken Bias 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Hoffman Heiken BiasはHeikin Ashiの純出来高モデルと複数の移動平均線を組み合わせてトレンド方向を測定します。高速SMAが高速EMAを上回り、すべての長期移動平均がその下に留まり、純出来高の回帰が正のときにロングポジションをオープンします。逆の条件でショートが発動されます。

## 詳細

- **エントリー条件**:
  - **ロング**: `SMA(5) > EMA(18)` かつ すべての長期移動平均が `EMA(18)` を下回り かつ 純出来高回帰 > 0。
  - **ショート**: `SMA(5) < EMA(18)` かつ すべての長期移動平均が `EMA(18)` を上回り かつ 純出来高回帰 < 0。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `Fast SMA` = 5
  - `Fast EMA` = 18
  - `Net volume length` = 25
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SMA, EMA, ATR, Linear Regression
  - ストップ: なし
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
