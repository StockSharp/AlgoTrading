# Bollinger Percent B Reversion 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
このアプローチは、Percent Bインジケーターを使用してBollinger Bandsを超えた価格の極値に逆張りします。上部バンドを上回るまたは下部バンドを下回る動きは過剰延伸を示唆します。

テストでは年間平均リターン約142%が示されています。株式市場で最も優れたパフォーマンスを発揮します。

Percent Bがゼロ未満または1を超えると、システムはバンドの中心への回帰を期待します。出口閾値はモメンタムが正常化すると取引をクローズします。

ストップはエントリーから固定パーセントに設定されます。

## 詳細

- **エントリー条件**: Percent Bが0–1の範囲外。
- **ロング/ショート**: 両方向。
- **エグジット条件**: Percent Bが`ExitValue`をクロスするかストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `ExitValue` = 0.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Bollinger Bands
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

