# Bollinger Band Squeeze Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie repliziert den ursprünglichen MetaTrader 4-Expert Advisor "BOLINGER BAND SQUEEZE" mit der StockSharp High-Level-API. Sie sucht nach Perioden, in denen sich Bollinger Bands zusammenziehen, und tritt dann in Trades ein, sobald sich die Bänder ausdehnen, sofern Momentum- und Trendfilter die Bewegung bestätigen. Die Konvertierung behält die Multi-Timeframe-Bestätigungslogik bei und transformiert die Money-Management-Blöcke in StockSharp-Idiome.

## Handelslogik
1. **Band-Squeeze und Ausdehnung**
   - Bollinger Bands (Länge 20, Abweichung 2 standardmäßig) werden auf dem Arbeits-Zeitrahmen berechnet.
   - Die Breite der zuletzt abgeschlossenen Kerze wird gegen die Breite `RetraceCandles` Bars zuvor verglichen.
   - Ein gültiger Ausbruch erfordert, dass die Breitenverhältnis `SqueezeRatio` übersteigt, was signalisiert, dass der Preis aus dem Squeeze herausexpandiert.
2. **Trendfilter**
   - Zwei gewichtete gleitende Durchschnitte (WMA 6 und WMA 85 auf Typical Price) definieren den sofortigen Trend. Long-Trades erfordern, dass der schnelle WMA über dem langsamen WMA liegt, Shorts das Gegenteil.
3. **Momentum-Bestätigung**
   - Ein höherzeitiger Momentum-Indikator (Länge 14) prüft, ob der Preis ausreichend vom 100-Niveau abweicht. Die maximale Abweichung der letzten drei höherzeitigen Werte muss den richtungsspezifischen Schwellenwert überschreiten.
   - Der höhere Zeitrahmen wird automatisch ausgewählt, um das im MT4-Skript verwendete Mapping zu entsprechen (z.B. M15 → H1, H1 → D1, D1 → monatlich). Wochendaten fallen auch auf monatliche Bestätigung zurück. Wenn kein höherer Zeitrahmen verfügbar ist, wird der Momentum-Filter übersprungen.
4. **Makro-Filter**
   - Ein monatliches MACD (12/26/9) stellt sicher, dass längerfristiges Momentum mit der Handelsrichtung übereinstimmt (MACD-Linie über Signal für Longs, darunter für Shorts).
5. **Einstiegsregeln**
   - Longs: Band-Ausdehnung, schneller WMA über langsamem WMA, monatlicher MACD bullisch, höherzeitiger Momentum-Abweichung über `MomentumBuyThreshold`, und struktureller Kerzen-Überlapp (`candle[-2].Low < candle[-1].High`).
   - Shorts: Band-Ausdehnung, schneller WMA unter langsamem WMA, monatlicher MACD bärisch, Momentum-Abweichung über `MomentumSellThreshold`, und gespiegelte Kerzenbedingung (`candle[-1].Low < candle[-2].High`).
6. **Ausstiegsregeln**
   - Positionen werden geschlossen, wenn der Preis am äußeren Bollinger Band in der Handelsrichtung schließt oder darüber hinausgeht (d.h. Long-Ausstiege am oberen Band, Short-Ausstiege am unteren Band), entsprechend der MT4-Implementierung.
   - `StartProtection()` aktiviert StockSharp's Schutzorder-Infrastruktur, sodass Stop-Loss/Take-Profit-Erweiterungen bei Bedarf hinzugefügt werden können.

## Indikatoren und Daten-Abonnements
- Primäre Zeitrahmen-Kerzen definiert durch `CandleType`.
- Höhere Zeitrahmen-Kerzen für Momentum-Bestätigung (automatisch vom Basis-Zeitrahmen gemappt).
- Monatliche Kerzen für MACD-Filterung (30-Tage-Approximation).
- Indikatoren: Bollinger Bands, zwei gewichtete gleitende Durchschnitte (Typical Price), Momentum und MovingAverageConvergenceDivergenceSignal.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 15-Minuten-Kerzen | Primärer Arbeits-Zeitrahmen. |
| `BollingerPeriod` | 20 | Bollinger Band-Länge. |
| `BollingerWidth` | 2.0 | Bollinger Band-Standardabweichungs-Multiplikator. |
| `SqueezeRatio` | 1.1 | Minimales Breitenausdehnungsverhältnis zwischen aktuellen und historischen Bändern. |
| `RetraceCandles` | 10 | Rückblick für Squeeze-Vergleich. |
| `FastMaLength` | 6 | Länge des schnellen WMA (Typical Price). |
| `SlowMaLength` | 85 | Länge des langsamen WMA (Typical Price). |
| `MomentumLength` | 14 | Momentum-Periode im höheren Zeitrahmen. |
| `MomentumBuyThreshold` | 0.3 | Minimale Abweichung von 100 zur Validierung von Long-Einstiegen. |
| `MomentumSellThreshold` | 0.3 | Minimale Abweichung von 100 zur Validierung von Short-Einstiegen. |

Alle Parameter sind als `StrategyParam<T>`-Werte exponiert und können in StockSharp Designer oder zur Laufzeit optimiert werden.

## Implementierungshinweise
- Die Strategie verwendet `SubscribeCandles().BindEx(...)`, um das Indikator-Wiring deklarativ zu halten und manuelle Indikator-Sammlungen zu vermeiden, wie von den High-Level-API-Richtlinien verlangt.
- Gewichtete gleitende Durchschnitte werden durch Typical Price innerhalb des Kerzenverarbeitungs-Callbacks angetrieben, um das Verhalten der LWMA-Berechnungen im MT4-Skript zu bewahren.
- Höherzeitige Momentum-Werte werden in einer Drei-Element-Queue gespeichert, um `iMomentum`-Rückblicke 1–3 aus dem Originalcode zu imitieren.
- Monatliche MACD-Werte persistieren in Klassen-Feldern, damit jede primäre Zeitrahmen-Kerze Zugriff auf den neuesten Langzeit-Bias hat.
- Ausstiege, die durch die äußeren Bänder ausgelöst werden, ersetzen die MT4-Trailing-Stop/Break-Even-Blöcke, behalten aber die visuelle Absicht des Schließens bei, wenn der Preis die entgegengesetzte Hülle berührt.
- Die Strategie überlässt die Ordergrößenbestimmung dem Basis-`Strategy.Volume`. Positionsflips kompensieren automatisch jede bestehende Exponierung, indem `Math.Abs(Position)` zum Ordervolumen hinzugefügt wird.
