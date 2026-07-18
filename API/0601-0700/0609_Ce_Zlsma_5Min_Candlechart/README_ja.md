# CE ZLSMA 5MIN Candlechart戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Heikin Ashiローソク足上のZLSMAとChandelier Exitフィルターを使用したトレンドフォローシステムです。トレンドが強気に転換し、ローソク足がZLSMAを上回って終値を形成した場合に買います。

## 詳細

- **エントリー条件**:
  - ロング: 方向が上向きに転換し、Heikin Ashiの終値がZLSMAと始値を上回っている
- **ロング/ショート**: ロング
- **エグジット条件**:
  - ロング: ZLSMAを下回る終値
- **ストップ**: なし
- **デフォルト値**:
  - `ZlsmaLength` = 50
  - `AtrPeriod` = 1
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング
  - インジケーター: ZLSMA, ATR, Heikin Ashi
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
