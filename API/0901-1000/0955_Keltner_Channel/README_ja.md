# ケルトナーチャネル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はケルトナーチャネルのブレイクアウトとEMAトレンドクロスを取引します。

## 詳細

- **エントリー条件**:
  - ロング: 価格がケルトナー下限バンドを下抜けするか、EMA9がEMA21を上抜けしながら価格がEMA50より上にある。
  - ショート: 価格がケルトナー上限バンドを上抜けするか、EMA9がEMA21を下抜けしながら価格がEMA50より下にある。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - 価格が反対方向に中間バンドを越えるか、EMAが逆方向にクロスする。
  - ストップロス: 1.5 ATR。
  - テイクプロフィット: 3 ATR。
- **ストップ**: あり。
- **デフォルト値**:
  - `Length` = 20
  - `Multiplier` = 1.5
  - `AtrMultiplier` = 1.5
  - `FastEmaPeriod` = 9
  - `SlowEmaPeriod` = 21
  - `TrendEmaPeriod` = 50
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: チャネル
  - 方向: 両方
  - インジケーター: EMA, ATR, Keltner
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
