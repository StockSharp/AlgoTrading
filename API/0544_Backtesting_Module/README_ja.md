# バックテストモジュール
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はTradingViewの「Backtesting Module」のデフォルト動作を再現します。シンプルな移動平均クロスオーバーを取引します。50期間SMAが200期間SMAを上抜けたときにロングポジションを建て、逆のクロスオーバーが発生したときにショートポジションを建てます。取引は指定された開始時刻と終了時刻の間のみ許可されます。

## 詳細

- **エントリー条件**: 50期間SMAが200期間SMAをクロス。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆クロスオーバーまたは時間区間の離脱。
- **ストップ**: なし。
- **デフォルト値**:
  - `FastLength` = 50
  - `SlowLength` = 200
  - `StartTime` = 1 Jan 1980
  - `EndTime` = 31 Dec 2050
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 可変
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
