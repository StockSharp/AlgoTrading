# Forex Fire EMA MA RSI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMA、MA、RSI確認を使用したマルチ時間軸トレンド戦略。コンフルエンスに4hローソク足、エントリーに15mローソク足を使用します。

## 詳細

- **エントリー条件**:
  - ロング: 短期EMAが長期EMAを上回り、価格がMAを上回り、高速RSIが低速RSIを上回りかつ>50、出来高が増加し上位時間軸で確認。
  - ショート: 逆の条件。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - EMAクロスまたはRSIが閾値に到達。
  - オプションのストップロス、テイクプロフィット、トレーリングストップ、ATRベースの決済。
- **ストップ**: あり、設定可能。
- **デフォルト値**:
  - `EmaShortLength` = 13
  - `EmaLongLength` = 62
  - `MaLength` = 200
  - `MaType` = MovingAverageTypeEnum.Simple
  - `RsiSlowLength` = 28
  - `RsiFastLength` = 7
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `UseStopLoss` = true
  - `StopLossPercent` = 2
  - `UseTakeProfit` = true
  - `TakeProfitPercent` = 4
  - `UseTrailingStop` = true
  - `TrailingPercent` = 1.5
  - `UseAtrExits` = true
  - `AtrMultiplier` = 2
  - `AtrLength` = 14
  - `EntryCandleType` = TimeSpan.FromMinutes(15).TimeFrame()
  - `ConfluenceCandleType` = TimeSpan.FromHours(4).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, MA, RSI, ATR
  - ストップ: はい
  - 複雑さ: 中
  - 時間軸: マルチ時間軸
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
