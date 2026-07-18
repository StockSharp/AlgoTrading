# MA PSAR ATRトレンド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MA PSAR ATRトレンド戦略は、移動平均クロスオーバーと日足パラボリックSARフィルターを組み合わせます。価格が両方の平均の上方または下方に揃い、PSARが同意する場合にのみ取引を行います。ATRベースのストップがリスクを管理します。

このメソッドは、動的ストップを使用したトレンドフォローを求めるトレーダーに適しています。シグナルはデフォルトで5分足ローソク足で発動します。

## 詳細
- **エントリー条件**:
  - **ロング**: 高速MA > 低速MA、終値 > 高速MA、安値 > 日足PSAR
  - **ショート**: 高速MA < 低速MA、終値 < 高速MA、高値 < 日足PSAR
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - **ロング**: トレンドが弱気に転じるか価格がATRストップを下回る
  - **ショート**: トレンドが強気に転じるか価格がATRストップを上回る
- **ストップ**: はい、ATRベース。
- **デフォルト値**:
  - `FastMaPeriod` = 40
  - `SlowMaPeriod` = 160
  - `SarStep` = 0.02m
  - `SarMaxStep` = 0.2m
  - `AtrPeriod` = 14
  - `AtrMultiplierLong` = 2m
  - `AtrMultiplierShort` = 2m
  - `UsePsarFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MA, Parabolic SAR, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
