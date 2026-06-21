# DCA サポート・レジスタンス RSIトレンドフィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

サポート・レジスタンスレベル、RSI、およびEMAトレンドフィルターを使用したドルコスト平均戦略。RSIが売られすぎの状態で上昇トレンド中にサポートで買い、RSIが買われすぎの状態で下降トレンド中にレジスタンスで売ります。

## 詳細

- **エントリー条件**:
  - ロング: サポートで価格、RSIが売られすぎ以下、EMAの上
  - ショート: レジスタンスで価格、RSIが買われすぎ以上、EMAの下
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: 価格がレジスタンスに達するかRSIが買われすぎ以上
  - ショート: 価格がサポートに達するかRSIが売られすぎ以下
- **ストップ**: なし
- **デフォルト値**:
  - `LookbackPeriod` = 50
  - `RsiLength` = 14
  - `Overbought` = 70
  - `Oversold` = 40
  - `EmaPeriod` = 200
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: RSI, EMA, Highest, Lowest
  - ストップ: いいえ
  - 複雑さ: 初心者
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
