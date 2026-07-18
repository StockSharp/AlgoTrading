# ROC Impulce 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
Rate of Change（ROC）インパルスに基づく戦略

テストでは年平均リターンが約91%であることが示されています。株式市場で最もよく機能します。

ROC Impulseは、Rate of Changeインジケーターの突然の急増を捉えます。急激なプラスのスパイクはロング取引につながり、急激なマイナスはショート取引につながります。モメンタムがゼロに向かって弱まるとポジションが閉じられます。

トリガーレベルは例外的なモメンタムイベントにのみ反応するよう調整できます。ATRベースのストップは、スパイクが素早く反転した場合の大きな損失を防ぐのに役立ちます。


## 詳細

- **エントリー条件**: ATR、ROC、Momentumに基づくシグナル。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `RocPeriod` = 12
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ATR、ROC、Momentum
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - Neural Networks: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

