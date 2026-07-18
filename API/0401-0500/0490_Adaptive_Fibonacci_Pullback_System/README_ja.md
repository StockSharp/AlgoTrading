# アダプティブ Fibonacci プルバック戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Fibonacci 乗数 (0.618、1.618、2.618) で構築された 3 本の SuperTrend ラインを平均化し、その結果を EMA で平滑化します。このアダプティブトレンドへのプルバックに従ってトレードを行い、AMA ベースの中央線とオプションの RSI フィルターで方向を確認します。

## 詳細

- **エントリー条件**:
  - 安値が平均 SuperTrend を下回り、終値がその平滑化された値を上回る。
  - AMA 中央線に対する前の終値がプルバックを定義する。
  - **ロング**: 終値が中央線を上回り、RSI > しきい値。
  - **ショート**: 終値が中央線を下回り、RSI < しきい値。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - 終値が平滑化された SuperTrend を逆方向に横切る。
- **ストップ**: `StartProtection` によるパーセンテージストップロスとテイクプロフィット。
- **デフォルト値**:
  - `AtrPeriod` = 8
  - `SmoothLength` = 21
  - `AmaLength` = 55
  - `RsiLength` = 7
  - `RsiBuy` = 70
  - `RsiSell` = 30
  - `TakeProfitPercent` = 5
  - `StopLossPercent` = 0.75
- **フィルター**:
  - カテゴリ: トレンドプルバック
  - 方向: 両方
  - インジケーター: SuperTrend, EMA, AMA, RSI
  - ストップ: はい
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
