# BBSR Extreme

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bandeintritt mit EMA-Steigungsfilter und ATR-Schutz. `C,L,U,E` sind Rohschlusskurs, unteres Band, oberes Band und EMA; `p` bezeichnet die vorherige gebildete Kerze.

## Handelsregeln

Long: `C[p] < L[p] && C > L && E > E[p]`. Short: `C[p] > U[p] && C < U && E < E[p]`. Gleichheit genügt nicht. Einstieg nur ohne Position mit `Volume`, ohne Aufstockung, Umkehr oder feste Kerzenpause.

Vor dem Einstieg wird `A=ATR` der Signalkerze eingefroren. Nativer lokaler Schutz verwendet den tatsächlichen Ausführungspreis: Long Stop/Ziel `fill-2A` / `fill+3A`, Short `fill+2A` / `fill-3A`. Multiplikatoren sind Parameter; spätere ATR-Werte verschieben die Ebenen nicht. Bid/Ask-Abonnements ermöglichen Marktabschlüsse des tatsächlich ausgeführten Volumens zwischen Signalkerzen.

Einstiege verwenden nur abgeschlossene Kerzen und vollständig gebildete Indikatoren. Offene Positionen werden bis zum Ausstieg verwaltet; ausstehende Marktorders verhindern doppelte Aufträge.

Bollinger verwendet die Populationsstandardabweichung des gleitenden Schlusskursfensters einschließlich der aktuellen abgeschlossenen Kerze.

## Parameter

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `20` |
| `BollingerMultiplier` | `2` |
| `MaLength` | `7 (EMA)` |
| `AtrLength` | `14 (Wilder)` |
| `AtrStopMultiplier` | `2` |
| `AtrProfitMultiplier` | `3` |
| `CandleType` | `5m` |
