# DCA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、選択した曜日または設定したバー数後に、定期的に固定の建玉通貨額を購入します。指定した終了日以降にすべてのポジションをオプションで決済します。

### 実際の購入日程と数量

既定値: `CandleType=5m`、`CapitalToInvest=200` **建値通貨単位**、`StartDate=DateTime.MinValue`、`EndDate=DateTime.MaxValue`、`CloseAllOnLastCandle=false`、`UseDayOfWeek=false`、`DayOfWeek=Wednesday`、`EveryXCandles=72`。

確定足のうち `StartDate <= 足の日付 < EndDate` の範囲で取引します。間隔モードは**有効な確定足72本ごと**に購入、曜日モードは**毎週水曜日の最初の有効な確定足**で日付につき1回購入。`200 / Close` の数量を `Security.VolumeStep` に**切り捨て**、商品ごとの最小/最大取引数量を守ります。EMAクロスやショートは使わず、**成行買い**を積み増します。EndDate以降の新規購入は停止し、`CloseAllOnLastCandle=true` なら最初の有効な足で**実残高全量**を1回売却。独立した価格ストップはなく、スリッページで実際の購入金額は変化し得ます。元のREADMEに数値既定値がなかったため、これらは実装時に決めた値です。



## パラメータ
- ローソク足タイプ
- 取引ごとの投資資金
- 開始日
- 終了日
- 最終ローソクで全決済
- 曜日モードを使用
- 曜日
- Xバーごと
