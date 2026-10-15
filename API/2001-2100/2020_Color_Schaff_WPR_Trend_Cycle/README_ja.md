# Color Schaff WPR トレンドサイクル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTraderの**Color Schaff WPR Trend Cycle**エキスパートを実装しています。
高速および低速のWilliams %R値から計算されたSchaff Trend Cycleを使用して、市場の転換点を検出します。

アルゴリズムは確定したローソク足のみで動作します。インジケーター値が上限レベルを上抜けすると、戦略はロングポジションを開き、既存のショートポジションをすべて閉じます。値が下限レベルを下抜けすると、ショートポジションを開き、既存のロングポジションをすべて閉じます。

## パラメーター
- **Fast WPR** – 高速Williams %R計算の期間。
- **Slow WPR** – 低速Williams %R計算の期間。
- **Cycle** – Schaff Trend計算に使用するサイクルの長さ。
- **High Level** – ロングエントリーの上限トリガーレベル。
- **Low Level** – ショートエントリーの下限トリガーレベル。
- **Candle Type** – インジケーター評価に使用するローソク足の時間軸。

## リンク
- オリジナルMQLソース: `MQL/13489/mql5/Experts/exp_colorschaffwprtrendcycle.mq5`
- インジケーター: `MQL/13489/mql5/Indicators/colorschaffwprtrendcycle.mq5`
