# XMAレンジチャネル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

高値と安値の移動平均から上部・下部チャネルを構築する戦略です。上部バンドを上抜けするとロングエントリーが発生し、下部バンドを下抜けするとショートエントリーが発生します。このモデルはオリジナルMQLエキスパート「XMA Range Channel」の動作を再現します。

## 詳細

- **エントリー条件**:
  - ロング: `Close > UpperChannel`
  - ショート: `Close < LowerChannel`
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナル
- **ストップ**: なし
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Length` = 7
- **フィルター**:
  - カテゴリ: チャネルブレイクアウト
  - 方向: 両方
  - インジケーター: High/LowのSMA
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: スイング
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
