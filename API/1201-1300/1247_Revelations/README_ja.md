# Revelations戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

局所的な極値とレジーム指数で確認された強いATRスパイク時にエントリーするボラティリティブレイクアウト戦略。ポジションサイズはスパイクの強度に適応する。

## 詳細

- **エントリー条件**:
  - **ロング**: レジーム確認を伴う局所安値でのATR上方スパイク。
  - **ショート**: レジーム確認を伴う局所高値でのATR下方スパイク。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 利益確定またはストップロスの到達。
- **ストップ**: 固定パーセントストップ。
- **デフォルト値**:
  - `ATR Fast` = 14
  - `ATR Slow` = 21
  - `ATR StdDev` = 12
  - `Spike Threshold` = 0.5
  - `Super Spike Mult` = 1.5
  - `Regime Window` = 8
  - `Regime Events` = 3
  - `Local Window` = 3
  - `Max Quantity` = 2
  - `Min Quantity` = 1
  - `Stop %` = 0.9
  - `Take Profit %` = 1.8
- **フィルター**:
  - カテゴリ: ボラティリティブレイクアウト
  - 方向: ロング/ショート
  - インジケーター: ATR, SMA, Highest/Lowest
  - ストップ: はい
  - 複雑さ: 上級
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
