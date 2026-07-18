# Zwei MA, anderer Zeitrahmen, korrekte Kreuzungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader 5-Expertenberaters „Two MA Other TimeFrame Correct Intersection“. Das ursprüngliche EA basiert auf zwei gleitenden Durchschnitten, die jeweils in einem eigenen Zeitrahmen berechnet werden (z. B. H1 vs. D1), während Handelsentscheidungen mit dem Zeitrahmen des Diagramms synchronisiert werden. Die Konvertierung behält das Multi-Timeframe-Verhalten bei und eröffnet Long-Positionen, wenn der schnelle gleitende Durchschnitt den langsam gleitenden Durchschnitt überschreitet. Umgekehrt werden Short-Positionen eröffnet, wenn der schnelle Durchschnitt den langsamen unterschreitet. Alle Aufträge werden zum Marktpreis ausgeführt und die Strategie schließt immer alle gegenteiligen Risiken, bevor ein neuer Handel eröffnet wird, was dem motorgesteuerten Ausführungsmodell des MQL5-Skripts entspricht.

## Handelslogik
- Abonnieren Sie drei Candle-Streams: den primären Handelszeitrahmen, den Fast-MA-Zeitrahmen und den Slow-MA-Zeitrahmen.
- Berechnen Sie die schnellen und langsamen gleitenden Durchschnitte für die jeweiligen Zeitrahmen. Jeder gleitende Durchschnitt unterstützt dieselben Glättungsmethoden und Preisquellen, die vom ursprünglichen `iCustom`-Indikator offengelegt wurden.
- Wenden Sie optional eine konfigurierbare horizontale Verschiebung auf die gleitenden Durchschnittsausgaben an, bevor sie verglichen werden, und reproduzieren Sie so die `ma_shift`-Eingaben von EA.
- Überprüfen Sie jedes Mal, wenn eine Kerze im primären Handelszeitraum endet, auf einen Schnittpunkt zwischen dem aktuellsten und dem vorherigen gleitenden Durchschnittswert:
  - Wenn der schnelle MA im vorherigen Schritt unter dem langsamen MA lag und jetzt darüber liegt, schließen Sie jede Short-Position und eröffnen Sie eine Long-Position (oder kehren Sie in diese um).
  - Wenn der schnelle MA im vorherigen Schritt über dem langsamen MA lag und jetzt darunter liegt, schließen Sie jede Long-Position und eröffnen Sie eine Short-Position (oder kehren Sie in diese um).
- Alle Einträge nutzen das konfigurierte Handelsvolumen. Beim Umkehren einer bestehenden Position erhöht die Strategie die Ordergröße um die Größe des entgegengesetzten Engagements, um sicherzustellen, dass die Position in einer einzigen Marktorder umgedreht wird.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Grundvolumen für Markteintritte. Wird sowohl für Long- als auch für Short-Trades verwendet. |
| `CandleType` | Primärer Handelszeitraum. Signale werden immer dann ausgewertet, wenn eine Kerze dieses Typs schließt. |
| `FastTimeFrame` | Zeitrahmen, der zur Bildung des schnellen gleitenden Durchschnitts verwendet wird. |
| `SlowTimeFrame` | Zeitrahmen, der zur Bildung des langsamen gleitenden Durchschnitts verwendet wird. |
| `FastLength` | Anzahl der im schnellen gleitenden Durchschnitt enthaltenen Balken. |
| `SlowLength` | Anzahl der im langsam gleitenden Durchschnitt enthaltenen Balken. |
| `FastShift` | Horizontale Verschiebung, die vor dem Vergleich auf die Ausgabe des schnellen gleitenden Durchschnitts angewendet wird. |
| `SlowShift` | Horizontale Verschiebung, die vor dem Vergleich auf die Ausgabe des langsam gleitenden Durchschnitts angewendet wird. |
| `FastMethod` | Glättungsalgorithmus für den schnellen gleitenden Durchschnitt (einfach, exponentiell, geglättet oder linear gewichtet). |
| `SlowMethod` | Glättungsalgorithmus für den langsam gleitenden Durchschnitt. |
| `FastAppliedPrice` | Kerzenpreis, der vom sich schnell bewegenden Durchschnitt verwendet wird (Eröffnung, Hoch, Tief, Schluss, Median, typisch oder gewichtet). |
| `SlowAppliedPrice` | Kerzenpreis, der vom langsam gleitenden Durchschnitt verwendet wird. |

## Hinweise zur Implementierung
- Die gleitenden Durchschnitte werden über StockSharp High-Level-Abonnements (`SubscribeCandles().Bind(...)`) verarbeitet und laufen auch dann weiter, wenn der Handelszeitrahmen vom Berechnungszeitrahmen abweicht.
- Verschiebungsparameter werden mit kleinen Warteschlangen implementiert, die die Indikatorausgabe um die angeforderte Anzahl von Balken verzögern und so das Verhalten der `ma_shift`-Eingaben reproduzieren.
- Die Strategie nutzt `StartProtection()` zur Abstimmung mit den Kontoschutzdienstprogrammen StockSharp, genau wie die ursprüngliche Handelsmaschine, die offene Positionen schützte.
- Bei der Diagrammdarstellung werden die primären Kerzen zusammen mit den schnellen und langsamen gleitenden Durchschnitten hinzugefügt, sodass die Crossover-Signale während Backtests sichtbar bleiben.
- Im ursprünglichen EA gibt es kein Stop-Loss-, Take-Profit- oder Trailing-Stop-Modul. Händler können dieses Modul mit separaten Money-Management-Strategien kombinieren, wenn zusätzliche Risikokontrolle erforderlich ist.
