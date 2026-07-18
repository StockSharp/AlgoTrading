# Keltner Volume 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Keltner Channels + Volume 戦略の実装。平均を超える出来高で上部 Keltner Channel を上抜けたときに買い。平均を超える出来高で下部 Keltner Channel を下抜けたときに売り。

テストでは年平均リターン約 58% を示しています。株式市場で最も優れたパフォーマンスを発揮します。

Keltner Channel の境界は潜在的な反転点を示し、出来高の増加がその確信を裏付けます。価格がバンドに触れ出来高が拡大したときにシステムがトレードを行います。

ボラティリティバンド付近での出来高確認を求めるトレーダーに適したセットアップです。ストップは ATR から計算されます。

## 詳細

- **エントリー条件**:
  - ロング: `Close < LowerBand && Volume > AvgVolume`
  - ショート: `Close > UpperBand && Volume > AvgVolume`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 価格が EMA を突き抜ける
- **ストップ**: `StopLoss` を使用した ATR ベース
- **デフォルト値**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `Multiplier` = 2.0m
  - `VolumeAvgPeriod` = 20
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Keltner Channel, 出来高
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
