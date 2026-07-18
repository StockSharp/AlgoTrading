# シンプルトレーディングシステム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMetaTraderのSimple Trading Systemを再現しています。数バー分シフトされた移動平均を使用し、現在の終値と以前の終値を比較して短期的なトレンド反転を検出します。移動平均が`MaShift`バー前の値を下回り、終値が`MaShift`バーと`MaPeriod + MaShift`バー前の終値の間にあり、ローソク足が陰線の場合に買いシグナルが発生します。売りシグナルはその鏡像です。パラメーターに応じて、シグナルが現れたときにロングまたはショートポジションを開閉できます。オプションのストップロスとテイクプロフィットのレベルを設定できます。

## 詳細

- **エントリー条件:**
  - **ロング**: `MA(t) <= MA(t+MaShift)` && `Close(t) >= Close(t+MaShift)` && `Close(t) <= Close(t+MaPeriod+MaShift)` && `Close(t) < Open(t)`
  - **ショート**: `MA(t) >= MA(t+MaShift)` && `Close(t) <= Close(t+MaShift)` && `Close(t) >= Close(t+MaPeriod+MaShift)` && `Close(t) > Open(t)`
- **ロング/ショート**: `BuyPositionOpen`と`SellPositionOpen`に応じた両方向。
- **エグジット条件**: `BuyPositionClose`または`SellPositionClose`が有効な場合、反対シグナルで決済がトリガーされます。
- **ストップ**: オプション。`StopLoss`と`TakeProfit`は`StartProtection`を通じた絶対価格単位。
- **デフォルト値:**
  - `MaType` = EMA
  - `MaPeriod` = 2
  - `MaShift` = 4
  - `PriceType` = Close
  - `CandleType` = 6時間ローソク足
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `Volume` = 1
- **フィルター:**
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 移動平均
  - ストップ: はい
  - 複雑さ: 中程度
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
