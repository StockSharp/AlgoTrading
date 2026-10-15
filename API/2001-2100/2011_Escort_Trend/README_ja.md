# Escort トレンド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Escort トレンド戦略は、高速・低速の加重移動平均（WMA）とMACD・CCIの確認を組み合わせます。高速WMAが低速WMAより上にあり、MACDメインラインがシグナルラインを上抜け、CCIが正のしきい値を超えるとロングポジションが開かれます。ショートポジションは逆の条件でトリガーされます。この戦略はオプションで固定のストップロス、テイクプロフィット、トレーリングストップを使用します。

## 詳細
- **エントリー条件**:
  - **ロング**: `FastWMA > SlowWMA` かつ `MACD > Signal` かつ `CCI > +Threshold`。
  - **ショート**: `FastWMA < SlowWMA` かつ `MACD < Signal` かつ `CCI < -Threshold`。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - 反対のエントリーシグナル。
  - オプションのストップロス、テイクプロフィット、またはトレーリングストップ。
- **ストップ**: はい、ユーザー定義。
- **デフォルト値**:
  - `Fast WMA` = 8
  - `Slow WMA` = 18
  - `CCI Period` = 14
  - `CCI Threshold` = 100
  - `MACD Fast EMA` = 8
  - `MACD Slow EMA` = 18
  - `Take Profit` = 200
  - `Stop Loss` = 55
  - `Trailing Stop` = 35
  - `Trailing Step` = 3
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 複数
  - ストップ: はい
  - 複雑さ: 中程度
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
