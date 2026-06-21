# Heikin Ashi ROCパーセンタイル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はローソク足をHeikin Ashiに変換し、SMAで終値を平滑化してそのRate of Changeを測定します。最近のROCの高値と安値のパーセンタイルバンドがブレイクアウトレベルを形成します。下バンドを上抜けするとロングを開くまたは反転し、上バンドを下抜けするとショートに反転します。

## 詳細

- **エントリー条件**:
  - ロング: ROCが下方パーセンタイルラインを上抜け。
  - ショート: ROCが上方パーセンタイルラインを下抜け。
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナルまたはストップ。
- **ストップ**: パーセントストップ。
- **デフォルト値**:
  - `RocLength` = 100
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
  - `StartDate` = new DateTimeOffset(2015, 3, 3, 0, 0, 0, TimeSpan.Zero)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Heikin Ashi, RateOfChange, Highest, Lowest
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
