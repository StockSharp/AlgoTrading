# Gold & EUR/USD 流動性グラブ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、RSI、SMA、ストキャスティクスオシレーター、ATRベースのフェアバリューギャップを使用してGoldとEUR/USDの需要・供給ゾーンにおける流動性グラブを検出します。

## 詳細

- **エントリー条件**:
  - **ロング**: 価格が直近安値を下抜ける影をつけ、市場構造が上向きに転換し、フェアバリューギャップが発生し、RSIが売られ過ぎ、価格がSMAを上回り、ストキャスティクスが売られ過ぎ。
  - **ショート**: 価格が直近高値を上抜ける影をつけ、市場構造が下向きに転換し、フェアバリューギャップが発生し、RSIが買われ過ぎ、価格がSMAを下回り、ストキャスティクスが買われ過ぎ。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `RsiLength` = 14
  - `MaLength` = 50
  - `StochLength` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `StochOverbought` = 80
  - `StochOversold` = 20
- **フィルター**:
  - カテゴリ: Price action
  - 方向: 両方
  - インジケーター: RSI, SMA, Stochastic, ATR, Highest, Lowest
  - ストップ: いいえ
  - 複雑さ: 中
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
