# Xmacd モード戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

4つの異なるエントリーモードをサポートするMACDインジケーターに基づく戦略：

- **Breakdown**: MACDがゼロラインをクロスしたときに取引を開始。
- **MacdTwist**: MACDの方向が下降から上昇、またはその逆に変化したときに反応。
- **SignalTwist**: シグナルラインの転換点をトリガーとして使用。
- **MacdDisposition**: MACDとシグナルラインのクロスオーバーで取引。

戦略は4時間ローソク足をサブスクライブし、クラシックなMACD（EMA 12/26、9期間シグナル）を計算します。逆のシグナルでポジションを開閉できます。リスクはエントリー価格の割合で表される任意のストップロスとテイクプロフィットによって管理されます。

## 詳細

- **エントリー条件**: 選択したモードに応じたMACDベースのシグナル。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆のシグナルまたはストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `CandleType` = TimeSpan.FromHours(4)
  - `Mode` = MacdDisposition
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MACD
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: スイング (4h)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
