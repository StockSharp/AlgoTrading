# NQ Phantom Scalper Pro 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

オプションの出来高・トレンドフィルター付きVWAPバンドブレイクアウト戦略。

## 詳細

- **エントリー条件**:
  - **ロング**: 価格が確認出来高を伴ってVWAP上限バンドの上に終値をつける。
  - **ショート**: 価格が確認出来高を伴ってVWAP下限バンドの下に終値をつける。
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 価格がVWAPを再び横切るか、ATRストップが発動する。
- **ストップ**: ATRベース
- **デフォルト値**:
  - `Band #1 Mult` = 1.0
  - `Band #2 Mult` = 2.0
  - `ATR Length` = 14
  - `ATR Stop Mult` = 1.0
  - `Volume SMA Period` = 20
  - `Volume Spike Mult` = 1.5
  - `Trend EMA Length` = 50
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: VWAP, ATR, EMA, SMA
  - ストップ: はい
  - 複雑さ: 中
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
