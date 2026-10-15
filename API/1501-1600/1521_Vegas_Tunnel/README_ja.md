# Vegas Tunnel戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

4本のEMAを使ってトンネルを定義し、オプションでATRベースのストップを使用します。
価格と速いEMAが遅いEMAとトンネルEMAの上にあるときにロング、下にあるときにショートを建てます。

## 詳細

- **エントリー条件**: トンネルに対する価格とEMAの整列
- **ロング/ショート**: 両方
- **エグジット条件**: ストップロスまたはテイクプロフィット
- **ストップ**: ATRまたはEMAベース
- **デフォルト値**:
  - `RiskRewardRatio` = 2
  - `UseAtr` = true
  - `AtrLength` = 14
  - `AtrMult` = 1.5
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
