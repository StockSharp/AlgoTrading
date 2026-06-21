# Exp ATRトレーリング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この例は、**Average True Range (ATR)** インジケーターに基づくトレーリングストップで既存ポジションを管理する方法を示しています。この戦略はエントリーシグナルを生成せず、市場のボラティリティに応じてオープンポジションのエグジットレベルを調整するだけです。

## 動作の仕組み

1. 戦略は選択した時間軸のローソク足データをサブスクライブする。
2. 各ローソク足で `AverageTrueRange` インジケーターを計算する。
3. ロングポジションではストップレベルを `Close - ATR * BuyFactor` まで引き上げる。
4. ショートポジションではストップレベルを `Close + ATR * SellFactor` まで引き下げる。
5. 価格がトレーリングレベルを突破した場合、ポジションを成行で決済する。

トレーリングストップはトレードの方向にのみ動き、後退することはなく、ボラティリティ調整済みのエグジットを提供します。

## パラメーター

| 名前 | 説明 |
| --- | --- |
| `AtrPeriod` | ATRの計算期間。 |
| `BuyFactor` | ロングポジションをトレーリングする際にATRに掛ける倍率。 |
| `SellFactor` | ショートポジションをトレーリングする際にATRに掛ける倍率。 |
| `CandleType` | 分析に使用するローソク足の時間軸。 |

## 使用上の注意

- 戦略をインストゥルメントに適用し、手動または別の戦略からポジションを開く。
- エントリーとは独立してエグジットを管理するリスク管理シナリオに適している。
- チャートエリアにはローソク足・ATR値・執行済みトレードが表示され、視覚的な分析が可能。

## 参考資料

- [StockSharpドキュメントのAverage True Range](https://doc.stocksharp.com/topics/indicator_average_true_range.html)
- [Strategy Designer](https://doc.stocksharp.com/topics/designer.html)
