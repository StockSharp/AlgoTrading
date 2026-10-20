# Anomalous Holonomy Field Theory

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sechsteiliger technischer Score: feste EMA20/EMA50, Wilder-RSI14, MACD(12,26,9), Wilder-ATR und volatilitätsnormalisierter ROC. `C` ist der Rohschluss, `A=ATR`, `clip(x,k)=max(-k,min(k,x))`. Fenster enthalten die aktuelle abgeschlossene Kerze.

## Handelsregeln

`T`: +1.5 über beiden EMA, +1 nur über EMA20, -1.5 unter beiden, -1 nur unter EMA20, sonst null. Kerzen-VWAP: `sum(((High+Low+Close)/3)*Volume)/sum(Volume)` über `VwapPeriod`. `V=clip((C-VWAP)/A,2)`. Für `B=T+V` addiert RSI +1.5 unter 30 oder +0.5 unter 40 nur bei `B>0`; -1.5 über 70 oder -0.5 über 60 nur bei `B<0`, sonst `R=0`.

`M=clip((MACD-EMA9(MACD))/A,1)`; `Q=clip((C-C[vor RocPeriod Kerzen])/A,1)`, entsprechend ROC/ATR% mit identischer historischer Preisbasis. `S=T+V+R+M+Q`. Ohne Position wird `Volume` bei `S>=SignalThreshold` gekauft, bei `S<=-SignalThreshold` verkauft. Fehlender Vorlauf, nichtpositiver ATR oder Nullvolumen sperren den Einstieg. Der Abstand der Einstiegsindizes muss strikt größer als `CooldownBars` sein.

Vor jedem Einstieg wird `ATR*AtrStopMultiplier` fixiert. Der native lokale Marktstop verankert diesen Abstand an tatsächlichen Ausführungen und reagiert zwischen Kerzen auf Bid/Ask; Standard 2 ATR. Bestehende Positionen verlassen nur über Schutz, ohne Aufstockung oder Umkehr bei Gegenscores. Einstiegspausen verzögern den Stop nicht. Kein Gewinnziel.

Einstiege verwenden nur abgeschlossene Kerzen und vollständig gebildete Indikatoren. Offene Positionen werden bis zum Ausstieg verwaltet; ausstehende Marktorders verhindern doppelte Aufträge.

## Parameter

| Parameter | Default |
|---|---|
| `SignalThreshold` | `2` |
| `CooldownBars` | `50` |
| `AtrLength` | `14` |
| `AtrStopMultiplier` | `2` |
| `RocPeriod` | `10` |
| `VwapPeriod` | `20` |
| `CandleType` | `5m` |
