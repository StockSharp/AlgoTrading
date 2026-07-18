# Cronex CCI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Cronex商品チャンネル指数のクロスオーバーに基づく戦略。インジケーターは2つの指数移動平均でCCIを平滑化し、高速ラインと低速ラインを作成します。

高速ラインが低速ラインを下向きにクロスすると戦略はロングポジションを開き、ショートポジションを閉じます。高速ラインが低速ラインを上向きにクロスするとショートポジションを開き、ロングポジションを閉じます。

この逆張りアプローチはモメンタム転換後の反転を捉えることを試みます。4時間ローソク足などの高い時間軸で機能します。

## 詳細

- **エントリー条件**: 高速・低速の平滑化CCIラインのクロスオーバー。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対方向のクロスオーバー。
- **ストップ**: なし。
- **デフォルト値**:
  - `CciPeriod` = 25
  - `FastPeriod` = 14
  - `SlowPeriod` = 25
  - `CandleType` = TimeSpan.FromHours(4)
  - `EnableLongEntry` = true
  - `EnableShortEntry` = true
  - `EnableLongExit` = true
  - `EnableShortExit` = true
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: CCI, EMA
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: スイング (4h)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
