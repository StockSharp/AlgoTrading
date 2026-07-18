# DCA 平均回帰 Bollinger Band 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格がBollinger Bandの下限バンドを下抜けたとき、または毎月1日目に固定ドル額を購入します。指定した日付にすべてのポジションを決済します。

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
