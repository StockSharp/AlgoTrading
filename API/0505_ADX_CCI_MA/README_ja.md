# ADX CCI MA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はADX、CCI、設定可能な移動平均を組み合わせて強いトレンドを取引します。

+DIが-DIを上抜け、CCI > 100かつADXが閾値を超えたとき（オプションで終値がMA上）に買います。-DIが+DIを上抜け、CCI < -100かつADXが閾値を超えたとき（終値がMA下）にショートします。

パーセンテージベースのストップロスとテイクプロフィットに加え、移動平均に逆らう複数のローソク足後に決済するオプションのMAリスク管理を含みます。

## 詳細

- **エントリー条件**: CCI極値とADX > `AdxThreshold`を伴う+DI/-DIクロス、オプションの終値vs MA。
- **ロング/ショート**: 両方。
- **エグジット条件**: ストップロスまたはテイクプロフィット到達、オプションのMAリスク管理。
- **ストップ**: はい、テイクプロフィットとストップロス。
- **デフォルト値**:
  - `EnableLong` = true
  - `EnableShort` = true
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CciPeriod` = 15
  - `AdxLength` = 10
  - `AdxThreshold` = 20m
  - `UseMaTrend` = true
  - `MaType` = MovingAverageTypeEnum.Simple
  - `MaLength` = 200
  - `UseMaRiskManagement` = false
  - `MaRiskExitCandles` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ADX, CCI, MA
  - ストップ: はい
  - 複雑さ: 中程度
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
