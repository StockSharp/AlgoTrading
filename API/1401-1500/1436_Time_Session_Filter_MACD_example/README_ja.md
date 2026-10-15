# 時間セッションフィルター - MACDの例
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MACDとトレンドEMAを組み合わせた時間セッションフィルターの使用を示す戦略。設定された時間帯にのみトレードします。

## 詳細

- **エントリー条件**: アクティブセッション内でMACDがシグナルを交差し、トレンドEMAに対する価格位置を確認。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対方向のクロスオーバー、または有効時のセッション終了。
- **ストップ**: なし。
- **デフォルト値**:
  - `SessionStart` = 11:00
  - `SessionEnd` = 15:00
  - `CloseAtSessionEnd` = false
  - `FastEmaPeriod` = 11
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `TrendMaLength` = 55
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MACD, EMA
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
