# Drei Indikatoren Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist eine StockSharp-Konvertierung des ursprünglichen **"Three indicators"** MQL5-Experten. Sie wertet drei klassische Oszillatoren — MACD, Stochastischer Oszillator und RSI — bei jeder abgeschlossenen Kerze des ausgewählten Zeitrahmens aus. Nur wenn alle Filter ausgerichtet sind, tritt die Strategie in eine Position ein, was sicherstellt, dass jeder Trade einer konsistenten Multi-Indikator-Bestätigung folgt.

## Handelslogik
1. **Kerzenrichtungsfilter** – vergleicht den Eröffnungspreis der aktuellen abgeschlossenen Kerze mit dem der vorherigen. Eine höhere Eröffnung begünstigt Long-Trades, eine niedrigere begünstigt Shorts.
2. **MACD-Steigungsfilter** – beobachtet die Steigung der MACD-Hauptlinie (Differenz zwischen dem aktuellen und dem vorherigen MACD-Hauptwert). Ein fallender MACD begünstigt Long-Positionen, ein steigender MACD begünstigt Shorts, genau wie im Quell-Experten.
3. **Stochastischer Biasfilter** – prüft, ob der %D-Wert unter oder über dem 50er-Mittelpunkt liegt. Werte unter 50 unterstützen Longs, Werte über 50 unterstützen Shorts.
4. **RSI-Biasfilter** – verwendet den RSI-Wert relativ zu 50. Werte unter 50 autorisieren Longs, Werte über 50 autorisieren Shorts.

Nur wenn **alle vier Filter** dieselbe Richtung unterstützen, wird die Strategie einen neuen Trade öffnen. Wenn ein entgegengesetztes Signal erscheint, während eine Position offen ist, dreht die Strategie sofort um, indem eine einzelne Marktorder gesendet wird, die das bestehende Engagement schließt und die neue Richtung öffnet, was das Verhalten der ursprünglichen MQL-Logik widerspiegelt.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen der an die Strategie gelieferten Kerzen. Standard: 1 Minute. |
| `TradeVolume` | Volumen beim Öffnen einer Position oder Umkehr zur entgegengesetzten Seite. |
| `MacdFastPeriod` | Länge der schnellen EMA innerhalb der MACD-Berechnung. |
| `MacdSlowPeriod` | Länge der langsamen EMA innerhalb der MACD-Berechnung. |
| `MacdSignalPeriod` | EMA-Länge für die MACD-Signallinie. |
| `MacdPriceType` | Angewendeter Preis für den MACD-Indikator (Close, Open, High, Low, Median, Typical, Weighted). |
| `StochasticKPeriod` | Rückblickperiode für die %K-Linie. |
| `StochasticDPeriod` | Glättungsperiode für die %D-Linie. |
| `StochasticSlowing` | Zusätzliche Glättung auf %K vor der %D-Berechnung. |
| `RsiPeriod` | Mittelungsperiode des RSI-Filters. |
| `RsiPriceType` | Angewendeter Preis für den RSI-Indikator. |

## Indikatoren
- **MACD (Moving Average Convergence Divergence)** – konfiguriert mit den benutzerspezifizierten schnellen, langsamen und Signallängen.
- **Stochastischer Oszillator** – verwendet die StockSharp-Implementierung mit konfigurierbaren %K/%D-Längen und Verlangsamung.
- **Relative Strength Index (RSI)** – liefert die abschließende Momentum-Bestätigung.

## Verhaltenshinweise
- Die Strategie verarbeitet nur **abgeschlossene Kerzen**, was die Stabilität im Vergleich zum tick-basierten Trigger des ursprünglichen Experten verbessert.
- Die 30-Sekunden-Pause der MQL-Version ist entfernt; Umkehrungen werden sofort mit der kombinierten Marktorder ausgegeben.
- Die stochastische Glättung verwendet die standardmäßige Moving-Average-Implementierung von StockSharp, die der standardmäßigen SMA-basierten Glättung des ursprünglichen Skripts entspricht.
- Die Preisquellenauswahl für MACD und RSI wird durch das `IndicatorAppliedPrice`-Enum bereitgestellt, das den in MetaTrader verfügbaren Optionen entspricht (Close, Open, High, Low, Median, Typical, Weighted).

## Risikomanagement
Es werden keine Stop-Loss- oder Take-Profit-Orders automatisch platziert. Das Positionsmanagement wird ausschließlich durch die Multi-Indikator-Umkehrlogik gesteuert. Bei Bedarf externe Risikokontrollen hinzufügen.

## Nutzungstipps
1. Das gewünschte Instrument und den Zeitrahmen über `CandleType` auswählen.
2. Indikatorparameter anpassen, um zur Volatilität des Markts und der Signalfrequenz zu passen.
3. Die von der Strategie hinzugefügten Diagrammobjekte (Kerzen plus die drei Indikatoren) überwachen, um die Signalausrichtung zu validieren.
4. Mit externem Geldmanagement kombinieren, wenn feste Stops oder Gewinnziele erforderlich sind.
