# ZMFX Stolid 5a EA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIとStochasticの数値で確認された押し目でエントリーするマルチタイムフレームのトレンドフォロー戦略です。
4時間足のStochasticと1時間足の平滑移動平均から主要トレンドを識別します。
RSIの売られすぎ/買われすぎ条件でのローソク足の反転でポジションを建て、反対のシグナルでクローズします。

## 詳細

- **エントリー条件**:
  - ロング: `UpTrend && PreviousBarDown && PrevRSI < 30 && (RSI15 < 30 => double volume)`
  - ショート: `DownTrend && PreviousBarUp && PrevRSI > 70 && (RSI15 > 70 => double volume)`
- **ロング/ショート**: 両方
- **ストップ**: 明示的なストップなし。インジケーター条件によりポジションをクローズ
- **デフォルト値**:
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: RSI, Stochastic, Smoothed Moving Average
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: マルチタイムフレーム
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
