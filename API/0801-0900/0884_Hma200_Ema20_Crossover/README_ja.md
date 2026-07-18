# HMA 200 + EMA 20 クロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、価格が200期間のHull Moving Averageを上回り、20期間のExponential Moving Averageを上抜けたときにロングエントリーします。価格がHMAを下回りEMAを下抜けたときにショートポジションをオープンします。逆のシグナルでポジションを転換します。

## 詳細

- **エントリー条件**:
  - **ロング**: `Close > HMA` かつ `Close` が `EMA` を上抜け。
  - **ショート**: `Close < HMA` かつ `Close` が `EMA` を下抜け。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆方向のクロスオーバーシグナルで転換。
- **ストップ**: なし。
- **デフォルト値**:
  - `HMA Length` = 200
  - `EMA Length` = 20
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: HMA, EMA
  - ストップ: なし
  - 複雑さ: シンプル
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
