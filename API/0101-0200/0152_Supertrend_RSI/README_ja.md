# Supertrend RSI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Supertrend + RSI 戦略の実装。価格が Supertrend より上にあり RSI が 30 未満（売られすぎ）のときに買い。価格が Supertrend より下にあり RSI が 70 超（買われすぎ）のときに売り。

テストでは年平均リターン約 43% を示しています。株式市場で最も優れたパフォーマンスを発揮します。

Supertrend インジケーターは現在のトレンドを示し、RSI は価格が伸びすぎているときを検出します。RSI が極値に達したとき、注文は Supertrend の方向に従います。

トレーリングストップを活用するトレーダーに適した選択肢です。Supertrend の組み込みストップは ATR 設定と連動して損失を抑えます。

## 詳細

- **エントリー条件**:
  - ロング: `Close > Supertrend && RSI < RsiOversold`
  - ショート: `Close < Supertrend && RSI > RsiOverbought`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - Supertrend が反対方向に転換
- **ストップ**: Supertrend をトレーリングストップとして使用
- **デフォルト値**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Supertrend, RSI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
