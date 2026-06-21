# Elliott's Quadratic Momentumモメンタム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Elliott's Quadratic Momentum**戦略は、Elliott波動にインスパイアされたモメンタムを捉えるために複数のSuperTrendインジケーターを組み合わせます。

4本のSuperTrend線がすべて上昇トレンドを示したときにロング、すべて下降トレンドを示したときにショートでエントリーします。いずれかのSuperTrendが方向を反転したときにポジションをクローズします。

## 詳細
- **エントリー条件**: 全SuperTrendインジケーターが同じ方向に揃っている。
- **ロング/ショート**: 両方向。
- **エグジット条件**: いずれかのSuperTrendがポジションと逆に転換。
- **ストップ**: 明示的なストップなし。
- **デフォルト値**:
  - `AtrLength1 = 7`
  - `Multiplier1 = 4.0m`
  - `AtrLength2 = 14`
  - `Multiplier2 = 3.618m`
  - `AtrLength3 = 21`
  - `Multiplier3 = 3.5m`
  - `AtrLength4 = 28`
  - `Multiplier4 = 3.382m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SuperTrend
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
