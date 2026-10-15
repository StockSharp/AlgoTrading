# アダプティブ・スクイーズ・モメンタム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

アダプティブ・スクイーズ・モメンタム戦略は、ボリンジャーバンドがケルトナーチャネルの内側に収まるときのボラティリティ収縮を検出し、強いモメンタムを伴うブレイクアウトを待ちます。モメンタムの強さは標準偏差ベースのしきい値で評価されます。オプションの RSI と EMA トレンドフィルターがエントリーを精査します。ATR を使用して動的なストップロスとテイクプロフィットレベルを設定でき、ポジションは時間ベースの保有期間後に決済されます。

## 詳細

- **エントリー条件**:
  - スクイーズが解放される (ボリンジャーバンドがケルトナーチャネルの外側)。
  - **ロング**: モメンタム > 動的しきい値、RSI が売られすぎを上抜け、トレンド EMA が上昇 (オプション)。
  - **ショート**: モメンタム < -動的しきい値、RSI が買われすぎを下抜け、トレンド EMA が下降 (オプション)。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - 逆シグナル、ATR ベースのストップロス/テイクプロフィット、または時間ベースのエグジット。
- **ストップ**: オプションの ATR ストップロスとテイクプロフィット。
- **デフォルト値**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2.0
  - `KeltnerPeriod` = 20
  - `KeltnerMultiplier` = 1.5
  - `MomentumLength` = 12
  - `TrendMaLength` = 50
  - `UseAtrStops` = True
  - `AtrMultiplierSl` = 1.5
  - `AtrMultiplierTp` = 2.5
  - `AtrLength` = 14
  - `MinVolatility` = 0.5
  - `HoldingPeriodMultiplier` = 1.5
  - `UseTrendFilter` = True
  - `UseRsiFilter` = True
  - `RsiLength` = 14
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `MomentumMultiplier` = 1.5
  - `AllowLong` = True
  - `AllowShort` = True
- **フィルター**:
  - カテゴリ: ボラティリティブレイクアウト
  - 方向: 両方
  - インジケーター: Bollinger Bands, Keltner Channels, Momentum, RSI, EMA, ATR
  - ストップ: オプション
  - 複雑さ: 高
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
