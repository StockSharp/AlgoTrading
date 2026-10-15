# Crypto Scalper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Crypto Scalper-Strategie reproduziert die ursprüngliche MetaTrader-Expertenlogik mit StockSharp-High-Level-Komponenten. Sie überwacht einen bullischen oder bärischen Crossover eines schnellen linear gewichteten gleitenden Durchschnitts auf dem primären Zeitrahmen und bestätigt das Setup mit Trendfiltern, die auf einem höheren Zeitrahmen berechnet werden. Sobald die Bedingungen übereinstimmen, steigt die Strategie mit Market-Orders ein und verwaltet Ausstiege durch Stop-Loss- und Take-Profit-Abstände in MetaTrader-Pips.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Primary Candle` | Kerzentyp auf dem Haupt-Zeitrahmen verarbeitet. | 1-Minuten-Zeitrahmen |
| `Higher Candle` | Höherer Zeitrahmen-Kerzentyp für Bestätigung. | 15-Minuten-Zeitrahmen |
| `Fast LWMA` | Länge des primären linear gewichteten gleitenden Durchschnitts. | 8 |
| `Higher Fast MA` | Schnelle LWMA-Länge auf dem Bestätigungs-Zeitrahmen. | 6 |
| `Higher Slow MA` | Langsame LWMA-Länge auf dem Bestätigungs-Zeitrahmen. | 85 |
| `Momentum Period` | Momentum-Indikatorlänge auf höheren Zeitrahmen-Kerzen. | 14 |
| `Momentum Threshold` | Minimale Abweichung vom Referenz-Momentum (MetaTrader-Basislinie 100) für den Handel. | 0.3 |
| `Momentum Reference` | Referenzniveau zur Emulation des MetaTrader-Momentum-Skalierens. | 100 |
| `Stop Loss (pips)` | Schutz-Stop-Distanz in MetaTrader-Pips. | 20 |
| `Take Profit (pips)` | Schutz-Gewinn-Distanz in MetaTrader-Pips. | 50 |
| `Volume` | Ordervolumen in Lots. | 0.01 |
| `MACD Fast` | Schnelle EMA-Periode für MACD-Bestätigung. | 12 |
| `MACD Slow` | Langsame EMA-Periode für MACD-Bestätigung. | 26 |
| `MACD Signal` | Signal-EMA-Periode für MACD-Bestätigung. | 9 |

## Trading-Logik
1. Den primären Zeitrahmen abonnieren und eine LWMA berechnen, die schnell auf den Preis reagiert.
2. Einen Einstieg erkennen, wenn die vorherige Kerze die LWMA nach oben (Long) oder nach unten (Short) kreuzt.
3. Den Crossover mit den höheren Zeitrahmen-Filtern bestätigen:
   - Höherer schneller LWMA muss über dem höheren langsamen LWMA für Long-Einstiege und darunter für Short-Einstiege bleiben.
   - MACD-Histogramm (Haupt minus Signal) muss für Longs positiv und für Shorts negativ sein.
   - Momentum muss vom Referenzniveau um mindestens `Momentum Threshold` abweichen.
4. Eine Market-Order in der erkannten Richtung senden, wenn keine anderen Orders aktiv sind und die aktuelle Position es erlaubt.
5. Folgekerzen überwachen und die Position schließen, wenn der Stop-Loss- oder Take-Profit-Preis berührt wird.

## Hinweise
- Die Strategie verwendet StockSharp-High-Level-Subscriptions mit `Bind` und vermeidet manuelle Indikatorbuffer.
- Schutzlevel werden bei jeder Kerze unter Verwendung des Wertpapier-Preisschritts neu berechnet. Ein Fallback-Schritt von `0.0001` wird angewendet, wenn das Instrument keinen konfigurierten Preisschritt aufweist.
- Es ist nur eine Position gleichzeitig erlaubt. Nachfolgende Signale werden ignoriert, bis der bestehende Trade abgeschlossen ist.
- Alle Inline-Kommentare innerhalb der C#-Implementierung sind gemäß den Repository-Richtlinien auf Englisch geschrieben.
