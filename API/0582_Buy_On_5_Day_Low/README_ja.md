# 5日間安値での買い戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Buy On 5 Day Low**戦略は、終値が直近5日間の安値を下回るとロングポジションを建てます。前のバーの高値を終値が上抜けると手仕舞います。取引は設定可能な時間ウィンドウに限定されます。

## 詳細
- **エントリー条件**: 終値が直近N本のキャンドルの最安値を下回る。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 終値が前の高値を上回る。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `LowestPeriod = 5`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `StartTime = new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero)`
  - `EndTime = new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)`
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロング
  - インジケーター: Lowest, High
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
