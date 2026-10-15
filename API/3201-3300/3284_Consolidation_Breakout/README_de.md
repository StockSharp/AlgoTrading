# Konsolidierungsausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert das Kernverhalten des ursprünglichen **Consolidation Breakout** Expert Advisors für MetaTrader. Sie sucht nach engen Konsolidierungen, die durch Momentum- und MACD-Filter bestätigt werden, und eröffnet anschließend eine Position in Ausbruchsrichtung. Risiko wird über feste Take-Profit- und Stop-Loss-Distanzen in Preisschritten (Pips) gesteuert.

## Funktionsweise

1. Der primäre Zeitrahmen wird durch den Parameter `CandleType` definiert. Alle Trend- und Konsolidierungsprüfungen werden auf diesen Kerzen bewertet.
2. Zwei linear gewichtete gleitende Durchschnitte (LWMAs), berechnet auf dem typischen Preis, liefern den Richtungsfilter. Long-Setups verlangen, dass die schnelle LWMA über der langsamen LWMA bleibt, während Short-Setups die entgegengesetzte Ausrichtung benötigen.
3. Eine Konsolidierung wird erkannt, wenn das Tief der Kerze vor zwei Bars unter dem Hoch der vorherigen Kerze bleibt (Long-Fall) oder wenn das vorherige Tief unter dem Hoch von vor zwei Bars liegt (Short-Fall). Dies spiegelt die Overlap-Bar-Logik der MQL-Version.
4. Momentum muss die Bewegung bestätigen. Der absolute Momentum-Wert (relativ zu null) muss den jeweiligen Kauf- oder Verkaufsschwellenwert überschreiten. Dies nähert den Momentum-Filter des ursprünglichen Expert Advisors um das Niveau 100 an.
5. Ein separater MACD, berechnet auf dem Zeitrahmen `MacdCandleType`, muss mit der Handelsrichtung übereinstimmen. Die Strategie prüft, ob die MACD-Linie der Signallinie sowohl auf der positiven als auch auf der negativen Seite der Achse vorausläuft, wodurch die Multi-Timeframe-Bestätigung aus dem Quellcode reproduziert wird.
6. Wenn alle Filter übereinstimmen und das Konto flat oder in Gegenrichtung positioniert ist, sendet die Strategie eine Marktorder mit der Größe `TradeVolume`. Schutzlevel werden sofort in Preisschritten neu berechnet, sodass Intrabar-Extreme Ausstiege auslösen können.
7. Jede abgeschlossene Kerze überwacht auch aktive Positionen. Wenn die Kerzenspanne Stop-Loss- oder Take-Profit-Niveau berührt, schließt die Strategie die Position zum Markt und setzt die Schutzziele zurück.

## Indikatoren

- Linear gewichteter gleitender Durchschnitt (schnell und langsam, typischer Preis)
- Momentum
- MACD (mit 12/26/9-Perioden auf einem höheren Zeitrahmen)

## Parameter

- `CandleType` - primärer Zeitrahmen für die Ausbruchserkennung.
- `MacdCandleType` - Zeitrahmen für den bestätigenden MACD-Filter.
- `FastMaPeriod` - Länge der schnellen LWMA.
- `SlowMaPeriod` - Länge der langsamen LWMA.
- `MomentumLength` - Rückblick für den Momentum-Filter.
- `MomentumBuyThreshold` - minimales positives Momentum für Long-Trades.
- `MomentumSellThreshold` - minimales negatives Momentum für Short-Trades (als absoluter Wert ausgedrückt).
- `StopLossPips` - Schutz-Stop-Distanz in Preisschritten.
- `TakeProfitPips` - Gewinnziel-Distanz in Preisschritten.
- `TradeVolume` - Volumen, das mit jeder Marktorder gesendet wird.

Die Standardwerte spiegeln den veröffentlichten Expert Advisor: LWMA-Perioden 6 und 85, Momentum-Länge 14, Kauf-/Verkaufsschwellen 0.3, Stop-Loss 20 Pips und Take-Profit 50 Pips. Passen Sie pipbasierte Distanzen an, wenn Sie Instrumente mit anderen Preisschritten handeln.

## Hinweise

- Trailing Stops, Break-even-Bewegungen und Money-Management-Module aus dem MQL-Skript werden bewusst weggelassen, damit die StockSharp-Portierung auf die Kernlogik des Ausbruchs fokussiert bleibt.
- Stellen Sie immer sicher, dass die ausgewählten Zeitrahmen von Ihrem Datenfeed unterstützt werden. Wenn der höhere Zeitrahmen nur spärliche Daten liefert, erwägen Sie einen niedrigeren `MacdCandleType`, um den MACD-Filter reaktionsfähig zu halten.
