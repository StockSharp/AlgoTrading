# Gemini トレンドフォローシステム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

200日SMAと年次Rate of Changeフィルターで確認された強い上昇トレンド内で、50日SMAへの押し目買いを行うトレンドフォロー戦略。

## 詳細

- **エントリー条件**: 確認済みの上昇トレンドにおいて、最近の押し目の後に価格がSMA 50を上回って回復した場合。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: SMA 50がSMA 200を下抜けするデスクロスまたはカタストロフィックストップ。
- **ストップ**: オプションのカタストロフィックストップ。
- **デフォルト値**:
  - `Sma50Length` = 50
  - `Sma200Length` = 200
  - `RocPeriod` = 252
  - `RocMinPercent` = 15m
  - `UseCatastrophicStop` = true
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング
  - インジケーター: SMA, RateOfChange, Lowest
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
