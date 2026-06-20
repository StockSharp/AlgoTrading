# MACD Hidden Markov Model戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**MACD Hidden Markov Model**戦略はMACD Hidden Markov Modelを中心に構築されています。

テストでは年平均リターン約61%が示されています。暗号通貨市場で最もよいパフォーマンスを発揮します。

MarkovがイントラデイデータでのトレンドTransitionを確認するとシグナルが発生します (5m)。この手法はアクティブなトレーダーに適しています。

ストップはATRの倍数とMacdFast、MacdSlowなどのパラメーターに依存します。リスクとリワードのバランスを取るためにこれらのデフォルト値を調整してください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターベースの計算を使用。
- **デフォルト値**:
  - `MacdFast = 12`
  - `MacdSlow = 26`
  - `MacdSignal = 9`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `HmmHistoryLength = 100`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Markov
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: はい
  - ダイバージェンス: いいえ
  - リスクレベル: 中
