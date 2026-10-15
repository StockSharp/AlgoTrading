# EPSI Multi SET戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

オリジナルのMQL4エキスパート *e-PSI@MultiSET* から変換されたブレイクアウト戦略です。
各ローソク足を監視し、価格が始値から指定した距離だけ動いた場合にエントリーします。
ポジションはテイクプロフィットとストップロスのレベルで管理され、取引はユーザーが定義した
時間ウィンドウ内でのみ許可されます。

## 詳細

- **エントリー条件**:
  - ロング: `High - Open >= MinDistance`
  - ショート: `Open - Low >= MinDistance`
- **ロング/ショート**: 両方
- **エグジット条件**: TakeProfit または StopLoss
- **ストップ**: はい
- **デフォルト値**:
  - `MinDistance` = 20
  - `TakeProfit` = 20
  - `StopLoss` = 200
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
  - `OpenHour` = 2
  - `CloseHour` = 20
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: なし
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
