# Donchian WMA クロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Donchianチャネルの安値が加重移動平均線を上回るクロスが、2025年のカレンダー年の間のみロングエントリーを発動します。テイクプロフィットレベルに到達したとき、WMAが下落する中でクロスオーバーが逆転したとき、または日付が2025年外になったときにポジションを決済します。

## 詳細

- **エントリー条件**:
  - ロング: `DonchianLow`が`WMA`を上抜けし、日付が2025年内
- **ロング/ショート**: ロングのみ
- **エグジット条件**:
  - `TakeProfitPercent`によるテイクプロフィット
  - `WMA`が下降中に`DonchianLow`が`WMA`を下抜け
  - 日付が2025年外
- **ストップ**: テイクプロフィットのみ
- **デフォルト値**:
  - `DonchianLength` = 7
  - `WmaLength` = 62
  - `TakeProfitPercent` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: ロング
  - インジケーター: Donchianチャネル、加重移動平均
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: 中期
  - 季節性: 2025年のみ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
