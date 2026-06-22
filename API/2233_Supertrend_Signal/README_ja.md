# Supertrend シグナル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、終値がSuperTrend線をクロスしたときにポジションを建てます。価格が線を上回るとロング取引が建てられ、価格が線を下回るとショート取引が開かれます。反対のシグナルは既存のポジションをクローズして反転させます。

SuperTrendインジケーターはAverage True Range（ATR）を使用して価格を追跡し、支配的なトレンドを定義します。パラメーターでATR期間、乗数、ローソク足の時間軸を設定できます。

## 詳細

- **エントリー条件**:
  - ロング: 終値がSuperTrendを上抜け
  - ショート: 終値がSuperTrendを下抜け
- **ロング/ショート**: ロングとショート
- **エグジット条件**:
  - 反対のSuperTrendクロスオーバー
- **ストップ**: なし
- **デフォルト値**:
  - `AtrPeriod` = 5
  - `Multiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SuperTrend（ATRベース）
  - ストップ: いいえ
  - 複雑さ: 初心者
  - 時間軸: 中期
  - 季節性: なし
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
