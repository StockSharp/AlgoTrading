# Fibonacci Retracement Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Fibonacci Retracement Momentum-Strategie** ist eine Konvertierung des ursprünglichen MetaTrader Expert Advisors "FIBONACCI.mq4" zur StockSharp High-Level-API. Die Strategie kombiniert Multi-Timeframe-Fibonacci-Retracement-Niveaus mit Momentum- und MACD-Filtern, um Pullback-Einstiege in Richtung des vorherrschenden Trends zu timen. Die primäre Handelslogik wird auf dem Basis-Zeitrahmen ausgeführt, während Bestätigungsdaten von höheren Aggregationsperioden abgeleitet werden.

Der Algorithmus wurde von Grund auf mit StockSharp-Idiomen neu geschrieben: Kerzenabonnements, Indikator-Bindings und die eingebauten Order-Management-Helfer. Die Trailing-Logik aus der Quell-EA wurde vereinfacht, um sich auf das Kern-Retracement-Ausbruchsverhalten zu konzentrieren, während die ursprüngliche Signalstruktur (Fibonacci-Touch + Momentum-Schub + Trend-Filter) beibehalten wird.

## Wie es funktioniert
1. **Primärer Zeitrahmen** — die Strategie abonniert die ausgewählten Basiskerzen (standardmäßig 15 Minuten) und berechnet zwei gewichtete gleitende Durchschnitte (schnell und langsam), um die lokale Richtung zu beurteilen.
2. **Fibonacci-Anker-Zeitrahmen** — der höhere Zeitrahmen (Standard: 1 Stunde) liefert die zuletzt abgeschlossene Kerze. Deren Hoch/Tief wird verwendet, um das 0%–100% Fibonacci-Retracement-Raster zu konstruieren. Derselbe Kerzenstrom speist einen Momentum-Indikator (Rückblick 14), und die absolute Abweichung vom neutralen Niveau 100 wird für die letzten drei Bars gespeichert.
3. **MACD-Filter-Zeitrahmen** — ein langfristiger MACD (Standard: 12/26/9) wird auf monatlichen Kerzen (30-Tage-Approximation) berechnet und fungiert als Trend-Bestätigungsfilter.
4. Bei jeder abgeschlossenen Basiskerze prüft der Algorithmus, ob der Preis auf ein Fibonacci-Niveau zurückgekehrt ist, während die vorherigen Schlusskurse auf der entgegengesetzten Seite dieses Niveaus blieben. Kombiniert mit gleitendem Durchschnitts-Alignment, Momentum-Impuls und MACD-Bestätigung wird ein Trade eröffnet.
5. Schutzausstiege hängen von Stop-Loss- und Take-Profit-Abständen in Preisschritten ab. Wenn der Preis sich gegen die Position bewegt oder das Ziel erreicht, wird die Position geglättet.

## Einstiegsregeln
### Long-Setup
- Die letzte Kerze des höheren Zeitrahmens definiert die Fibonacci-Niveaus; das Tief der aktuellen Basiskerze berührt oder durchdringt ein Niveau, während mindestens einer der drei vorherigen Schlusskurse darüber blieb.
- Der schnelle gewichtete gleitende Durchschnitt liegt über dem langsamen gewichteten gleitenden Durchschnitt im Basis-Zeitrahmen.
- Die Momentum-Abweichung `|Momentum - 100|` im höheren Zeitrahmen überschreitet den konfigurierten Schwellenwert für einen der letzten drei Werte.
- Die MACD-Hauptlinie liegt über der Signallinie im MACD-Zeitrahmen.
- Strukturelle Überprüfung: Das Hoch der vorherigen Basiskerze liegt über dem Tief von zwei Bars zuvor (spiegelt `Low[2] < High[1]` aus der EA wider).

### Short-Setup
- Das Hoch der aktuellen Basiskerze berührt ein Fibonacci-Niveau, während mindestens einer der letzten drei Schlusskurse darunter blieb.
- Der schnelle gewichtete gleitende Durchschnitt liegt unter dem langsamen gewichteten gleitenden Durchschnitt.
- Die Momentum-Abweichung überschreitet den Schwellenwert für eine der letzten drei Messungen.
- Die MACD-Hauptlinie liegt unter der Signallinie im MACD-Zeitrahmen.
- Strukturelle Überprüfung: Das Hoch der vorherigen Kerze liegt über dem Tief des unmittelbar vorhergehenden Bars (analoges `Low[1] < High[2]`).

### Positionsmanagement
- Wenn ein entgegengesetztes Signal erscheint, während eine Position offen ist, schließt die Strategie zuerst die bestehende Position und wartet auf den nächsten Bar, um die Umkehrung einzuleiten. Dies spiegelt das konservative Order-Handling des ursprünglichen MQL-Codes wider.

## Risikomanagement
- **Stop-Loss / Take-Profit** — in Vielfachen des Preisschritts des Instruments konfiguriert. Null deaktiviert den entsprechenden Ausstieg.
- **Einstiegspreis-Tracking** — der Ausführungspreis wird durch den Schlusskurs der Signalkerze approximiert und zur Berechnung der Schutzabstände verwendet.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `FastMaLength` | 6 | Länge des schnellen gewichteten gleitenden Durchschnitts im Basis-Zeitrahmen. |
| `SlowMaLength` | 85 | Länge des langsamen gewichteten gleitenden Durchschnitts. |
| `MomentumLength` | 14 | Momentum-Rückblick im Fibonacci-Zeitrahmen. |
| `MomentumThreshold` | 0.3 | Minimale absolute Abweichung von 100 zur Momentum-Validierung. |
| `StopLossSteps` | 20 | Stop-Loss-Abstand in Preisschritten (0 deaktiviert). |
| `TakeProfitSteps` | 50 | Take-Profit-Abstand in Preisschritten (0 deaktiviert). |
| `MacdFastLength` | 12 | Schnelle EMA-Länge innerhalb von MACD. |
| `MacdSlowLength` | 26 | Langsame EMA-Länge innerhalb von MACD. |
| `MacdSignalLength` | 9 | Signal-EMA-Länge innerhalb von MACD. |
| `CandleType` | 15-Minuten-Kerzen | Primärer Ausführungs-Zeitrahmen. |
| `FibonacciCandleType` | 1-Stunden-Kerzen | Zeitrahmen für Fibonacci-Anker und Momentum. |
| `MacdCandleType` | 30-Tage-Kerzen | Zeitrahmen für den MACD-Trendfilter. |

## Nutzungshinweise
- Passen Sie die Zeitrahmen-Parameter an das ursprüngliche EA-Mapping an (z.B. M5 → M30, M15 → H1). StockSharp erlaubt jeden Kerzentyp, einschließlich Range- oder Tick-Bars.
- Da die Strategie `ClosePosition()` zum Glätten verwendet, sollte die `Volume`-Eigenschaft der gewünschten Trade-Größe entsprechen (Standard: 1 Lot-Äquivalent).
- Die Konvertierung konzentriert sich auf indikatorgetriebene Logik; Geldmanagement-Extras aus der MQL-Version (Eigenkapital-Stop, Trailing nach Kontostand usw.) wurden absichtlich für Klarheit weggelassen. Sie können die Klasse mit zusätzlichem Schutz erweitern, indem Sie `ManageRisk` einbinden.
- Führen Sie die Strategie innerhalb von StockSharp Designer, Shell oder Runner mit den erforderlichen Marktdaten-Adaptern aus.
