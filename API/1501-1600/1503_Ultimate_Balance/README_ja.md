# Ultimate Balance 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Ultimate Balance戦略はROC、RSI、CCI、Williams %R、ADXを加重オシレーターに統合する。このオシレーターの移動平均がシグナルを生成する: 売られすぎレベルを上抜けるとロング発動、買われすぎレベルを下抜けるとポジション決済または反転。

## 詳細

- **エントリー条件**: オシレーターMAが`OversoldLevel`を上抜ける。
- **ロング/ショート**: 両方（`EnableShort`でショートをオプション設定）。
- **エグジット条件**: オシレーターMAが`OverboughtLevel`を下抜ける。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `WeightRoc` = 2
  - `WeightRsi` = 0.5
  - `WeightCci` = 2
  - `WeightWilliams` = 0.5
  - `WeightAdx` = 0.5
  - `EnableShort` = false
  - `OverboughtLevel` = 0.75
  - `OversoldLevel` = 0.25
  - `MaType` = SMA
  - `MaLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: ROC, RSI, CCI, WilliamsR, ADX
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
