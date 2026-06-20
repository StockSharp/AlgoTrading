# ベータ加重 MA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Beta Weighted MA (BWMA) 戦略は、ベータ分布を使用して最近の価格を重み付けし、alpha および beta パラメータでラグと平滑度を調整できる移動平均を生成します。この戦略は、価格が BWMA を上抜けたときにロングポジションを、下抜けたときにショートポジションを建てます。

## 詳細

- **エントリー条件**:
  - 価格が Beta Weighted Moving Average を上抜ける → ロングエントリー。
  - 価格が Beta Weighted Moving Average を下抜ける → ショートエントリー。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 逆のクロスが現在のポジションを決済し、逆方向のポジションを建てる。
- **ストップ**: なし。
- **デフォルト値**:
  - `Length` = 50
  - `Alpha` = 3
  - `Beta` = 3
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング/ショート
  - インジケーター: Beta Weighted Moving Average
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
