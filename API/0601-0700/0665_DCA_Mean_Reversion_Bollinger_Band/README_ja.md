# DCA 平均回帰 Bollinger Band 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格がBollinger Bandの下限バンドを下抜けたとき、または毎月1日目に固定ドル額を購入します。指定した日付にすべてのポジションを決済します。

### C# / Python の厳密な取引条件

既定値: `InvestmentAmount=200` **建値通貨単位**、`OpenDate=DateTime.MinValue`、`CloseDate=DateTime.MaxValue`、`StrategyMode=Combined`、`BollingerPeriod=20`、`BollingerMultiplier=2`、`CandleType=5分足`。**確定足**のボリンジャー下限は SMA20 から標準偏差2倍を引いた値です。Bollinger 買いは、前回の `Close >= 前回の下限` から現在の `Close < 現在の下限` に抜けた時。Monthly は指標が形成された後、**月に1回**最初の有効な確定足で購入します。`Bollinger` は下抜けのみ、`Monthly` は月次のみ、`Combined` は両方（同時なら注文1回）。`OpenDate <= 日付 < CloseDate` の間、数量は `floor((InvestmentAmount / Close) / VolumeStep) × VolumeStep` に切り捨て、数量制限に従います。**ロング積み増しのみ**でショートは行いません。CloseDate 到達後、最初の有効な足で**実際の残高を全量売却**し、その後の購入を停止します。価格ストップ、ショート、EMAクロスはなく、スリッページで約定金額が変わる場合があります。元のREADMEに数値の既定値がなかったため、この実装で定義しました。



## パラメータ
- `InvestmentAmount` - 毎回の投資金額
- `OpenDate` - 購入開始日
- `CloseDate` - 全ポジション決済日
- `StrategyMode` - BB平均回帰、月次DCA、または組み合わせ
- `BollingerPeriod` - Bollinger Bandsの期間
- `BollingerMultiplier` - 標準偏差の乗数
- `CandleType` - Bollinger計算の時間軸

## インジケーター
- Bollinger Bands
