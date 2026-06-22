# Hawaiian Tsunami Surfer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は突然のモメンタムスパイクを探し、それに逆張りで取引します。Momentumインジケーターを使用して1本のバーにわたる終値の変化率を計算します。変化率が小さな閾値を超えると、その動きは「津波」と見なされます。戦略は強い上昇スパイクの後に売り、強い下降スパイクの後に買います。StartProtectionを通じて価格ステップでプロテクティブなストップロスとテイクプロフィットが適用されます。

## 詳細

- **エントリー条件**:
  - モメンタムのパーセンテージが `TsunamiStrength` を超えたときに売り。
  - モメンタムのパーセンテージが `-TsunamiStrength` を下回ったときに買い。
- **ロング/ショート**: 両方向。
- **エグジット条件**: プロテクティブなストップロスまたはテイクプロフィット。
- **ストップ**: はい、StartProtectionを通じて。
- **デフォルト値**:
  - `MomentumPeriod` = 1
  - `TsunamiStrength` = 0.24
  - `TakeProfitPoints` = 500
  - `StopLossPoints` = 700
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Momentum
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
