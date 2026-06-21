# Color Zerolag JCCX 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MetaTraderのColorZerolagJCCXインジケーターに着想を得た戦略です。2本の単純移動平均を使って元のオシレーターを近似します。
高速平均が低速平均を下抜けたときにロング、高速平均が低速平均を上抜けたときにショートになります。

## 詳細

- **エントリー条件**:
  - ロング: `高速MAが低速MAを下抜け`
  - ショート: `高速MAが低速MAを上抜け`
- **ロング/ショート**: 両方
- **エグジット条件**: 反対シグナル
- **ストップ**: `StartProtection()`
- **デフォルト値**:
  - `FastPeriod` = 8
  - `SlowPeriod` = 21
  - `CandleType` = 4時間足
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 移動平均
  - ストップ: オプション
  - 複雑さ: 基本
  - 時間軸: スイング
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
