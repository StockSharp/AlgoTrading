# Cryptocurrency-Fibonacci-MAs-Strategie (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie portiert den MetaTrader Expert Advisor "Cryptocurrency Fibonacci MAs" auf die High-Level-API von StockSharp. Das System verfolgt einen Stapel Fibonacci-basierter exponentieller gleitender Durchschnitte (8/13/21/55), validiert Momentum auf einem höheren Zeitrahmen und bestätigt den Makrotrend mit einem monatlichen MACD-Filter, bevor Marktorders gesendet werden. Nur abgeschlossene Kerzen werden verarbeitet und alle Indikatoraktualisierungen erfolgen über die `Bind`/`BindEx`-Pipeline.

Im Vergleich zur MetaTrader-Version wurden folgende bewusste Anpassungen vorgenommen:
- Geldbasierter Take Profit, Equity Stop-out, kerzenweises Trailing und Break-even-Automatisierung wurden weggelassen. Die StockSharp-Portierung nutzt klassische pipbasierte Stop-Loss- und Take-Profit-Logik über `StartProtection`.
- Order-Pyramiding ist auf eine Nettoposition pro Richtung begrenzt. Umkehrungen schließen zuerst die Gegenexposure und spiegeln damit das genettete Positionsmodell von StockSharp.
- Multi-Timeframe-Daten werden über zusätzliche Kerzenabonnements statt über Ad-hoc-Indikatoranforderungen bei Bedarf bereitgestellt.

## Handelslogik
### Long-Einstieg
1. EMA-Ausrichtung: 8 > 13 > 21 > 55 auf dem Hauptzeitrahmen.
2. Momentum auf höherem Zeitrahmen: Die absolute Abweichung des 14-Perioden-Momentum vom neutralen Niveau 100 liegt für mindestens eine der letzten drei Kerzen des höheren Zeitrahmens über dem konfigurierten Kaufschwellenwert.
3. Monatlicher MACD-Filter: Die MACD-Hauptlinie liegt über der Signallinie.
4. Positionsfilter: Die aktuelle Nettoposition muss flat oder short sein und unter dem konfigurierten Maximalvolumen bleiben.

### Short-Einstieg
1. EMA-Ausrichtung: 8 < 13 < 21 < 55.
2. Momentum-Abweichung über dem Verkaufsschwellenwert bei mindestens einer der letzten drei Kerzen des höheren Zeitrahmens.
3. MACD-Hauptlinie unter ihrer Signallinie.
4. Nettoexposure muss flat oder long sein und innerhalb des `MaxPositions`-Limits bleiben.

### Ausstiegslogik
- `StartProtection` platziert Schutz-Stop-Loss- und Take-Profit-Orders, die in Pip-Distanzen ausgedrückt werden. In dieser Portierung wird keine zusätzliche Trailing- oder Break-even-Logik angewendet.
- Umkehrsignale senden die entgegengesetzte Marktordergröße, die zuerst die bestehende Position ausgleicht, bevor die neue Exposure aufgebaut wird.

## Multi-Timeframe-Zuordnung
Der höhere Zeitrahmen für den Momentum-Indikator spiegelt die ursprüngliche Koeffiziententabelle:

| Hauptzeitrahmen | Momentum-Zeitrahmen |
| --- | --- |
| 1 Minute | 15 Minuten |
| 5 Minuten | 30 Minuten |
| 15 Minuten | 1 Stunde |
| 30 Minuten | 4 Stunden |
| 1 Stunde | 1 Tag |
| 4 Stunden | 1 Woche |
| 1 Tag | 1 Monat |
| 1 Woche | 1 Monat |
| 1 Monat | 1 Monat |

Die MACD-Bestätigung läuft immer auf einer monatlichen 30-Tage-Annäherung.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Ordergröße in Lots. | 0.1 |
| `StopLossPips` | Stop-Loss-Distanz in Pips. | 20 |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. | 50 |
| `MomentumBuyThreshold` | Minimale absolute Momentum-Abweichung von 100 für Long-Trades. | 0.3 |
| `MomentumSellThreshold` | Minimale absolute Momentum-Abweichung von 100 für Short-Trades. | 0.3 |
| `MaxPositions` | Maximales Nettovolumen pro Richtung, ausgedrückt als Vielfaches von `TradeVolume`. | 1 |
| `CandleType` | Primärer Zeitrahmen für EMA-Berechnungen. | 1-Stunden-Kerzen |

## Nutzungshinweise
1. Binden Sie die Strategie an ein Symbol und wählen Sie über `CandleType` einen geeigneten Zeitrahmen.
2. Stellen Sie sicher, dass die Datenquelle sowohl den Hauptzeitrahmen als auch die abgeleiteten höheren Zeitrahmen (Momentum und monatlich) bereitstellen kann.
3. Passen Sie pipbasierte Risikoparameter an die Tickgröße des Instruments an. Der Helfer wandelt Pips über `Security.PriceStep` in Instrumentenschritte um.
4. Backtesting und Optimierung können Momentum-Schwellen und Stop-Distanzen mit den bereitgestellten Parameterbereichen feinabstimmen.
