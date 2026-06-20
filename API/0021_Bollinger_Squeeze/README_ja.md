# Bollinger Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
ボリンジャーバンドのスクイーズに基づく戦略

テストでは年平均リターンが約100%であることが示されています。外国為替市場で最もよく機能します。

Bollinger Squeezeは低ボラティリティを示す狭いバンド幅を待ちます。バンドの外側へのブレイクがその方向への取引を開始し、モメンタムが失われるか反対のブレイクが現れると退場します。

スクイーズ状態は来たるべきボラティリティの拡大を示唆します。一度発動すると、取引はブレイクアウトに乗り、ATRストップまたはバンドクロスオーバーで退場します。


## 詳細

- **エントリー条件**: Bollingerに基づくシグナル。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `SqueezeThreshold` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Bollinger
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - Neural Networks: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

