# Trend-Reversal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Trend-Reversal-Strategie ist ein richtungsorientiertes System, das versucht, Ausbrüche nach einem kurzfristigen Pullback innerhalb eines bestehenden Trends zu erfassen. Sie wurde aus dem MetaTrader Expert Advisor "Trend Reversal" portiert und für die High-Level-API von StockSharp neu geschrieben. Die Umwandlung bewahrt den Kern der Bestätigungskette (gleitende Durchschnitte, Momentum und MACD), ersetzt jedoch die ursprünglichen grafischen Linienfilter durch Preisüberlappungsprüfungen, die sich programmatisch einfacher reproduzieren lassen.

## Indikatorstapel
- **Linear gewichtete gleitende Durchschnitte (LWMA)** auf typischem Preis mit anpassbaren schnellen und langsamen Längen. Die schnelle Linie verfolgt den neuesten Swing, während die langsame Linie den dominanten Trend identifiziert.
- **Momentum-Oszillator**, berechnet auf demselben Zeitrahmen. Die Strategie zeichnet die absolute Distanz vom neutralen Niveau 100 für die letzten drei geschlossenen Kerzen auf, um die MetaTrader-Logik nachzubilden.
- **MACD-Signallinienpaar**, konfiguriert mit unabhängigen schnellen, langsamen und Signal-Längen. Die Histogrammrichtung wird als Bestätigung auf höherer Ebene für Long- und Short-Trades verwendet.

## Handelslogik
1. Auf eine abgeschlossene Kerze im konfigurierten Zeitrahmen warten. Teilweise gebildete Bars werden ignoriert.
2. Sicherstellen, dass beide LWMAs und der Momentum-Indikator vollständig ausgebildet sind. Ohne genügend Historie bleibt das System flat.
3. Eine rollierende Queue der drei jüngsten Momentum-Abweichungen von 100 führen. Ein Setup ist nur gültig, wenn mindestens einer dieser Werte den jeweiligen Kauf- oder Verkaufsschwellenwert überschreitet.
4. Verlangen, dass die Kerze vor zwei Bars ein tieferes Tief als das Hoch der vorherigen Kerze hat. Dadurch wird die "überlappende" Struktur des ursprünglichen EA nachgebildet, die eine enge Konsolidierung vor dem Ausbruch erkennt.
5. Richtungsfilter bewerten:
   - **Long:** schnelle LWMA über langsamer LWMA und MACD-Hauptwert über der Signallinie.
   - **Short:** schnelle LWMA unter langsamer LWMA und MACD-Hauptwert unter der Signallinie.
6. Das Nettopositionslimit beachten. Die Strategie steigt nur ein oder baut eine Position aus, wenn die absolute Exposure (aktuelle Position geteilt durch Handelsvolumen) unter dem konfigurierten Wert `MaxPositions` liegt.
7. Orders werden mit `BuyMarket()` oder `SellMarket()` gesendet, wodurch je nach aktueller Exposure teilweise oder vollständige Umkehrungen möglich sind.

## Risikomanagement
- Optionale **Take-Profit**- und **Stop-Loss**-Distanzen (in Preiseinheiten) können über den integrierten Schutzblock von StockSharp angehängt werden. Beide Niveaus sind deaktiviert, wenn ein Parameter auf null gesetzt ist.
- Diese Portierung enthält keinen automatischen Trailing Stop und keine Break-even-Anpassung. Diese Funktionen können bei Bedarf mit zusätzlichen Event-Handlern implementiert werden.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Primärer Zeitrahmen zum Erstellen der Kerzen. | 15-Minuten-Zeitrahmen |
| `FastLength` | Periode der schnellen LWMA. | 6 |
| `SlowLength` | Periode der langsamen LWMA. | 85 |
| `MomentumLength` | Periode des Momentum-Oszillators. | 14 |
| `MomentumBuyThreshold` | Minimale absolute Momentum-Abweichung (von 100), die ein Long-Setup validiert. | 0.3 |
| `MomentumSellThreshold` | Minimale absolute Momentum-Abweichung (von 100), die ein Short-Setup validiert. | 0.3 |
| `MacdFastLength` | Schnelle EMA-Periode im MACD-Filter. | 12 |
| `MacdSlowLength` | Langsame EMA-Periode im MACD-Filter. | 26 |
| `MacdSignalLength` | Signal-EMA-Periode im MACD-Filter. | 9 |
| `TakeProfit` | Take-Profit-Distanz in Preiseinheiten. Auf 0 setzen, um zu deaktivieren. | 50 |
| `StopLoss` | Stop-Loss-Distanz in Preiseinheiten. Auf 0 setzen, um zu deaktivieren. | 20 |
| `TradeVolume` | Ordervolumen in Lots. | 1 |
| `MaxPositions` | Maximale Anzahl von Handelsvolumen-Einheiten in der Nettoposition. | 1 |

## Nutzungshinweise
- Binden Sie die Strategie an ein Wertpapier mit gültigen Schritt- und Preisinformationen, damit Schutzorders korrekt funktionieren.
- Für multidirektionales Trading (Pyramiding oder Skalieren) erhöhen Sie `MaxPositions`. Die Strategie fügt weiter Positionen hinzu, solange die Filter gültig bleiben und die Exposure innerhalb dieses Limits bleibt.
- Backtests sollten mit demselben Kerzenzeitrahmen durchgeführt werden, den der Parameter `CandleType` angibt. StockSharp fordert beim Start der Strategie automatisch die passenden Daten an.
- Da die MetaTrader-Version von handgezeichneten Trendlinien abhing, ersetzt diese Neufassung diese Prüfungen durch eine deterministische Kerzenüberlappung. Dadurch bleibt das Verhalten zwischen Backtests und Live-Ausführung konsistent.

## Unterschiede zum ursprünglichen EA
- Trailing Stop, Break-even-Bewegungen und equitybasierte Notausstiege sind nicht implementiert, damit das Beispiel auf die zentrale Signalgenerierung fokussiert bleibt.
- Money-Management-Funktionen wie Lot-Multiplikation und Magic-Number-Filterung sind in StockSharp nicht erforderlich und wurden daher entfernt.
- Die MACD-Bestätigung verwendet denselben Zeitrahmen wie die Handelskerzen statt der ursprünglichen monatlichen Aggregation. Sie können das Multi-Timeframe-Setup nachbilden, indem Sie einen langsameren Kerzentyp abonnieren und den MACD-Filter an diese Subscription binden.

## Optimierungstipps
- Optimieren Sie zuerst die Längen der gleitenden Durchschnitte passend zum dominanten Marktzyklus und verfeinern Sie danach die Momentum-Schwellenwerte.
- Experimentieren Sie bei volatilen Instrumenten mit breiteren Stop-Loss- und Take-Profit-Distanzen. Da die Logik trendfolgend ist, verbessern größere Ausstiegspuffer häufig die Profitabilität.
- Überwachen Sie Drawdown-Statistiken während der Optimierungsläufe. Ein höheres `MaxPositions` kann die Reaktionsfähigkeit verbessern, vergrößert aber auch das Risiko.
