# BBTrend SuperTrend Decision戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、異なる長さを持つ2つのボリンジャーバンドから**BBTrend**値を導出し、SuperTrendの計算に入力します。SuperTrendの方向により、ロングまたはショートポジションを開くかどうかが決まります。オプションとして、パーセントベースの利益確定とストップロスの保護を有効にすることができます。

## 詳細

- **エントリー条件**:
  - ロング: SuperTrendの方向が上向き。
  - ショート: SuperTrendの方向が下向き。
- **ロング/ショート**: 両方、設定可能。
- **エグジット条件**:
  - SuperTrendの方向が逆転。
- **ストップ**: オプションのパーセントTP/SL。
- **デフォルト値**:
  - 短期BB長さ = 20、長期BB長さ = 50、StdDev = 2。
  - SuperTrend長さ = 10、ファクター = 7。
  - Take Profit = 30%、Stop Loss = 20%。
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Bollinger Bands, SuperTrend
  - ストップ: オプションTP/SL
  - 複雑さ: 中程度
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
