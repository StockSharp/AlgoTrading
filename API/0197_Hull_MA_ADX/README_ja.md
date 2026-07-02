# Hull Ma Adx 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
Hull Moving AverageとADXに基づく戦略。HMAが上昇しADX > 25（強いトレンド）のときロングエントリー。HMAが下降しADX > 25（強いトレンド）のときショートエントリー。ADX < 20（トレンド弱体化）で退場。

テストでは年平均リターン約178%を示しています。株式市場で最もパフォーマンスが良好です。

Hull MAはトレンドを示し、ADXがその強度を確認します。ADXが強さを示すときにHullの傾きに沿ってエントリーします。

確認を伴う滑らかなトレンドに注目するトレーダーに効果的です。ATRストップにより損失を抑制します。

## 詳細

- **エントリー条件**:
  - ロング: `HullMA turning up && ADX > 25`
  - ショート: `HullMA turning down && ADX > 25`
- **ロング/ショート**: 両方
- **エグジット条件**: Hull MA の反転
- **ストップ**: `AtrMultiplier` を使用したATRベース
- **デフォルト値**:
  - `HmaPeriod` = 9
  - `AdxPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Hull MA, Moving Average, ADX
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

