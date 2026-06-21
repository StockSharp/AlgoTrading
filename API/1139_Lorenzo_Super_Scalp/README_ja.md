# Lorenzo SuperScalp 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このスキャルピング戦略はRSI、ボリンジャーバンド、MACDを組み合わせています。RSIが45未満で価格が下限バンド付近にあり、MACDが上向きにクロスしたときに買います。RSIが55超で価格が上限バンド付近にあり、MACDが下向きにクロスしたときに売ります。取引間の最小バー数により急速な再エントリーを防ぎます。

## 詳細

- **エントリー条件**:
  - **ロング**: `RSI < 45` && `Close < LowerBand * 1.02` && `MACD` がシグナルを上抜け。
  - **ショート**: `RSI > 55` && `Close > UpperBand * 0.98` && `MACD` がシグナルを下抜け。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `RSI Length` = 14
  - `Bollinger Length` = 20
  - `Bollinger Multiplier` = 2
  - `MACD Fast` = 12
  - `MACD Slow` = 26
  - `MACD Signal` = 9
  - `Min Bars` = 15
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 複数
  - ストップ: いいえ
  - 複雑さ: 中程度
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
