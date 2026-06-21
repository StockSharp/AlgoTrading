# ショートのみ 10バー安値プルバック戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格が前の足の最安値を下回り、内部バー強度（IBS）が閾値を超えたときにショートに参入する戦略。オプションのEMAフィルターで下降トレンドを確認する。

## 詳細

- **エントリー条件**:
  - 安値が前の`LowestPeriod`本のバーの最安値を下回る。
  - IBS > `IbsThreshold`。
  - オプション: フィルターが有効な場合、終値がEMAを下回る。
  - `StartTime`から`EndTime`の間の時間帯。
- **ロング/ショート**: ショートのみ。
- **エグジット条件**:
  - 終値が前の安値を下回るとショートをクローズ。
- **ストップ**: なし。
- **デフォルト値**:
  - `LowestPeriod` = 10
  - `IbsThreshold` = 0.85
  - `UseEmaFilter` = true
  - `EmaPeriod` = 200
- **フィルター**:
  - カテゴリ: プルバック
  - 方向: ショート
  - インジケーター: Lowest, EMA
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
