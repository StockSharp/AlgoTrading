# Color Step Xccx戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Color Step XCCXインジケーターに基づく戦略です。インジケーターは平滑化された平均からの価格の偏差を測定し、2本のステップラインを描画します。速い線が遅い線を下回ると買いトレードが開かれます。速い線が遅い線を上回るとショートトレードが開かれます。

## 詳細

- **エントリー条件**:
  - ロング: 速い線が遅い線を下抜け
  - ショート: 速い線が遅い線を上抜け
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: 速い線が遅い線を上抜け
  - ショート: 速い線が遅い線を下抜け
- **ストップ**: なし
- **デフォルト値**:
  - `DPeriod` = 30
  - `MPeriod` = 7
  - `StepSizeFast` = 5
  - `StepSizeSlow` = 30
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Custom, EMA
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
