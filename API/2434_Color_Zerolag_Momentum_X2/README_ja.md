# Color Zerolag Momentum X2戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ゼロラグ移動平均クロスを使用したデュアル時間軸のモメンタム戦略です。上位の時間軸がトレンド方向を定義し、下位の時間軸でモメンタムがそのゼロラグ平均をトレンド方向にクロスした際にエントリーをトリガーします。

## 詳細

- **エントリー条件**: モメンタムがトレンド方向にゼロラグ平均をクロス
- **ロング/ショート**: 両方
- **エグジット条件**: 逆方向クロスまたはトレンド転換
- **ストップ**: なし
- **デフォルト値**:
  - `TrendCandleType` = 6h
  - `TrendMomentumPeriod` = 34
  - `TrendMaLength` = 15
  - `SignalCandleType` = 30m
  - `SignalMomentumPeriod` = 34
  - `SignalMaLength` = 15
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Momentum, ZeroLagEMA
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: マルチ時間軸
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
