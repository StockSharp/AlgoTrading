# Waddah Attar トレンド
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はオリジナルのMQLエキスパート「Exp_Waddah_Attar_Trend」をStockSharpの高レベルAPIに変換したものです。2つの指数移動平均（高速と低速）の差に追加の平滑化移動平均を乗じるWaddah Attar Trendインジケーターを使用します。インジケーターはカラー状態を出力します：トレンド値が上昇するときは緑、下落するときはマゼンタ。この色の変化がトレードをトリガーします。

色が下降から上昇に切り替わるとロングポジションが開かれます。上昇から下降に切り替わるとショートポジションが開かれます。戦略は両方向で機能し、エントリー価格に対するパーセンテージで表されたストップロスとテイクプロフィットをサポートします。

## 詳細

- **エントリー条件**: Waddah Attar Trendの色変化（MACD差分にMAを乗じたもの）。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対の色変化または保護ストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `MaLength` = 9
  - `SignalBar` = 1
  - `TrendMode` = Direct
  - `StopLossPercent` = 1.0
  - `TakeProfitPercent` = 2.0
  - `CandleType` = TimeSpan.FromHours(4)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MACD, MA
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: H4
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
