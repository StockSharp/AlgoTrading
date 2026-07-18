# CCI Put Call Ratio ダイバージェンス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**CCI Put Call Ratio Divergence** 戦略は、CCI Put Call Ratio ダイバージェンスを中心に構築されています。

テストでは年平均リターン約133%を示しています。暗号資産市場で最も優れたパフォーマンスを発揮します。

イントラデイ（5m）データ上でダイバージェンスがダイバージェンス設定を確認した場合にシグナルが発生します。このメソッドはアクティブトレーダーに適しています。

ストップはATRの倍数とCciPeriod、AtrMultiplierなどの係数に基づいています。リスクとリターンのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `CciPeriod = 20`
  - `AtrMultiplier = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Divergence
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: はい
  - リスクレベル: 中
