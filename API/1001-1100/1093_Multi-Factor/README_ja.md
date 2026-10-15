# マルチファクター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

マルチファクター戦略は、MACD、RSI、2本の移動平均線を組み合わせてトレンド確認付きで取引します。MACDラインがシグナルを上回り、RSIが70未満、価格が50期間SMAを上回り、50 SMAが200 SMAを上回るときにロングトレードが発生します。ショートトレードは逆の条件を使用します。

ストップとターゲットはATRの倍数に基づいています。

## 詳細

- **エントリー条件**:
  - **ロング**: `MACD > Signal` && `RSI < 70` && `Close > SMA50` && `SMA50 > SMA200`.
  - **ショート**: `MACD < Signal` && `RSI > 30` && `Close < SMA50` && `SMA50 < SMA200`.
- **ロング/ショート**: 両方向。
- **エグジット条件**: ATRベースのストップロスとテイクプロフィット。
- **ストップ**: はい。
- **デフォルト値**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `RsiLength` = 14
  - `AtrLength` = 14
  - `StopAtrMultiplier` = 2
  - `ProfitAtrMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(15)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: MACD, RSI, SMA, ATR
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
