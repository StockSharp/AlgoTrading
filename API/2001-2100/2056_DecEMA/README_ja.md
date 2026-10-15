# DecEMA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

DecEMAインジケーターを使用してトレンドの方向を追う戦略。このインジケーターは10回の連続した指数平滑化を適用して組み合わせ、遅延の少ない移動平均を作成します。戦略は直近3つのDecEMA値を比較します。ラインが上向きになり最新値が前の値を超えると、買いを行い任意のショートポジションを決済します。ラインが下向きになり最新値が前の値を下回ると、売りを行い任意のロングポジションを決済します。

## 詳細

- **エントリー条件**:
  - ロング: DecEMAの傾きが上向きになり現在値 > 前値
  - ショート: DecEMAの傾きが下向きになり現在値 < 前値
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: 傾きが下向きに転換
  - ショート: 傾きが上向きに転換
- **ストップ**: なし
- **デフォルト値**:
  - `EmaPeriod` = 3
  - `Length` = 15
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: DecEMA
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
