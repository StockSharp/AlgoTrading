# Color Zerolag DeMarker Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie konvertiert den ursprünglichen MQL5-Experten `Exp_ColorZerolagDeMarker` in das StockSharp-Framework. Sie verwendet eine benutzerdefinierte Kombination mehrerer **DeMarker**-Indikatoren, um schnelle und langsame Trendlinien zu erstellen. Handelssignale werden erzeugt, wenn sich diese Linien kreuzen.

## Indikatoren

- Fünf DeMarker-Indikatoren mit verschiedenen Perioden: 8, 21, 34, 55 und 89.
- Jeder Indikatorwert wird mit einem Gewichtungsfaktor multipliziert (0.05, 0.10, 0.16, 0.26 und 0.43).
- Die gewichteten Werte werden addiert, um die **schnelle** Linie zu bilden.
- Die **langsame** Linie ist eine exponentiell geglättete Version der schnellen Linie, gesteuert durch den Parameter `Smoothing`.

## Handelslogik

1. Kerzen mit einem konfigurierbaren Zeitrahmen abonnieren.
2. Bei jeder abgeschlossenen Kerze schnelle und langsame Linien berechnen.
3. Wenn die vorherige schnelle Linie über der vorherigen langsamen Linie lag und die aktuelle schnelle Linie unter die langsame fällt:
   - Short-Positionen schließen, wenn erlaubt.
   - Eine Long-Position eröffnen, wenn aktiviert.
4. Wenn die vorherige schnelle Linie unter der vorherigen langsamen Linie lag und die aktuelle schnelle Linie über die langsame steigt:
   - Long-Positionen schließen, wenn erlaubt.
   - Eine Short-Position eröffnen, wenn aktiviert.
5. Optionale Stop-Loss- und Take-Profit-Prozentsätze werden für neu eröffnete Positionen angewendet.

## Parameter

- `CandleTimeframe` – Zeitrahmen für Kerzenabonnement.
- `Smoothing` – Glättungsfaktor für die langsame Linie.
- `Factor1`–`Factor5` – Gewichtungsfaktoren für jede DeMarker-Periode.
- `DeMarkerPeriod1`–`DeMarkerPeriod5` – Perioden für DeMarker-Indikatoren.
- `Volume` – Auftragsvolumen.
- `OpenBuy` / `OpenSell` – Long/Short-Einstiege aktivieren.
- `CloseBuy` / `CloseSell` – Ausstiege für Long/Short-Positionen aktivieren.
- `StopLossPct` / `TakeProfitPct` – optionales prozentbasiertes Risikomanagement.

## Hinweise

Die Strategie arbeitet nur auf geschlossenen Kerzen und verwendet die High-Level StockSharp-API (`SubscribeCandles` und `Bind`). Alle Kommentare im Code sind zur Klarheit auf Englisch.
