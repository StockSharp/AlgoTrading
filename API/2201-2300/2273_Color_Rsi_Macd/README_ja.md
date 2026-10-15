# Color RSI MACD戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、4つの異なるモードで分析できるMACDインジケーターのシグナルを取引します:

- **Breakdown** – MACDヒストグラムがゼロラインを越えたときに取引。
- **MACD Twist** – MACDラインが方向を変えたときに取引。
- **Signal Twist** – シグナルラインが方向を変えたときに取引。
- **MACD Disposition** – MACDラインとシグナルラインのクロスで取引。

各モードは対応するフラグを使用して、ロングとショートのポジションを独立して開いたり閉じたりできます。

デフォルトではストップロスもテイクプロフィットも使用しません。

## 詳細

- **エントリー条件**: インジケーターシグナル
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `CandleType` = 4時間
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `Mode` = MACD Disposition
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: MACD
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
