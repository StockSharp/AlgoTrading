# Williams VIX Fix戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Williams VIX Fix戦略は、ラリー・ウィリアムズのボラティリティ指標を公表されたVIXが
存在しない銘柄に適応させます。参照期間における最高終値と現在の安値の距離を使って
合成VIX値を計算します。この値がBollinger Bandの閾値を上回るか、価格がBollinger
Bandの下限を下回って終値を付けると、この戦略は売られすぎの機会と見なします。逆算
によって買われすぎの極値を測ります。

このアプローチはボラティリティスパイク後の平均回帰を探します。VIX Fixが高い恐怖を
示し価格が下限バンドを下回っている時、ロング取引が開かれます。逆に、逆VIX Fixが
極端な油断を示し価格が上限バンドを上回っている時、既存のロングポジションが閉じられます。
パーセンタイルの閾値が感度を制御します。

## 詳細

- **エントリー条件**:
  - VIX Fix ≥ 上限バンドまたはパーセンタイル かつ 価格 < Bollinger Bandの下限。
- **ロング/ショート**: ロングエントリーと反対シグナルでのエグジット。
- **エグジット条件**:
  - 逆VIX Fix ≥ 上限バンドまたはパーセンタイル かつ 価格 > Bollinger Bandの上限。
- **ストップ**: なし。
- **デフォルト値**:
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `WvfPeriod` = 20
  - `WvfLookback` = 50
  - `HighestPercentile` = 0.85
  - `LowestPercentile` = 0.99
- **フィルター**:
  - カテゴリ: ボラティリティ平均回帰
  - 方向: ロング
  - インジケーター: Bollinger Bands、Williams VIX Fix
  - ストップ: いいえ
  - 複雑さ: 中
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
