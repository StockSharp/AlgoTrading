# トレンド確認と独立ストップロス付きCBC戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はカラーバーチェンジ（CBC）の状態を使用して、価格が前のローソク足の高値または安値を突破した際の転換点を検出します。エントリーはEMAとVWAPによるトレンド確認を必要とし、取引セッション時間帯に限定されます。エグジットはATRベースの利益目標を適用し、前のローソク足の極値をストップロスレベルとして使用します。

## 詳細

- **エントリー条件**: CBCの転換、オプションの強い転換フィルター、VWAPに対する低速EMA、取引時間内。
- **ロング/ショート**: 両方。
- **エグジット条件**: ATR乗数によるテイクプロフィット、前のローソク足の高値/安値のストップロス。
- **ストップ**: はい。
- **デフォルト値**:
  - `AtrLength` = 14
  - `ProfitTargetMultiplier` = 1.0m
  - `StrongFlipsOnly` = true
  - `EntryStartHour` = 10
  - `EntryEndHour` = 15
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, VWAP, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
