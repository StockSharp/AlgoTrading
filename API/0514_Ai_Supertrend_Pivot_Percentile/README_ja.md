# AI Supertrend ピボットパーセンタイル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、2 つの Supertrend インジケーターと ADX フィルター、Williams %R ピボットパーセンタイルフィルターを組み合わせます。価格が両方の Supertrend を上回り、ADX が強いトレンドを確認し、Williams %R が -50 を上回るときにロングポジションを開きます。ショートポジションは逆の条件を使用します。

## 詳細

- **エントリー条件**:
  - **ロング**: 価格が両方の Supertrend を上回り、ADX > しきい値、Williams %R > -50。
  - **ショート**: 価格が両方の Supertrend を下回り、ADX > しきい値、Williams %R < -50。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 逆シグナル。
- **ストップ**: パーセントベースのテイクプロフィットとストップロス。
- **デフォルト値**:
  - `Length1` = 10
  - `Factor1` = 3
  - `Length2` = 20
  - `Factor2` = 4
  - `AdxLength` = 14
  - `AdxThreshold` = 20
  - `PivotLength` = 14
  - `TpPercent` = 2
  - `SlPercent` = 1
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SuperTrend, ADX, Williams %R
  - ストップ: あり
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
