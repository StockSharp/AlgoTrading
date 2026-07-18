# SimpleTrade-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine C#-Konvertierung des MetaTrader 5 Expert Advisors "SimpleTrade (barabashkakvns Edition)". Sie vergleicht den Eröffnungspreis der aktuellen Bar mit dem Eröffnungspreis drei Bars zuvor. Wenn das aktuelle Open höher ist, geht die Strategie long; andernfalls geht sie short. Jede Position wird nur eine abgeschlossene Kerze lang gehalten und mit einer festen Stop-Loss-Distanz in Pips abgesichert.

Die StockSharp-Implementierung abonniert die ausgewählte Kerzenserie über die High-Level-API und reagiert nur auf abgeschlossene Bars, um sicherzustellen, dass Entscheidungen auf abgeschlossenen Preisdaten basieren. Positionen werden beim nächsten Bar-Übergang oder früher geschlossen, wenn das Stop-Level innerhalb der Bar-Range berührt wird.

## Handelslogik
- **Einstieg**
  - Bei jeder abgeschlossenen Bar den Eröffnungspreis speichern und eine rollende Geschichte der letzten vier Opens pflegen.
  - Wenn keine offene Position vorhanden ist und mindestens vier Eröffnungspreise verfügbar sind, das neueste Open mit dem vor drei Bars aufgezeichneten vergleichen.
  - Eine Long-Position eingehen, wenn das aktuelle Open über dem Open drei Bars zuvor liegt; andernfalls eine Short-Position eingehen.
- **Ausstieg**
  - Jeder Trade ist durch ein Stop-Level geschützt, das als *StopLossPips × Pip-Größe* vom Einstiegs-Eröffnungspreis berechnet wird.
  - Auf der folgenden Bar wird die Position unabhängig vom Ergebnis geschlossen, um den ursprünglichen Expert Advisor zu replizieren, der einen Trade nie länger als eine Kerze hält.
  - Wenn das Bar-High (für Shorts) oder Low (für Longs) das Stop-Level durchdringt, versucht die Strategie, die Position sofort zum Marktpreis zu schließen.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `StopLossPips` | 120 | Abstand vom Einstiegs-Eröffnungspreis zum Schutz-Stop, gemessen in Pips. Der Code reproduziert das MetaTrader-Verhalten, indem er den Preisschritt für Symbole mit 3 oder 5 Dezimalstellen mit 10 multipliziert. |
| `TradeVolume` | 1 | Order-Volumen für Market-Einstiege. Anpassen, um mit der Kontraktgröße des gehandelten Instruments übereinzustimmen. |
| `CandleType` | 1-Stunden-Zeitrahmen | Gibt an, welche Kerzenserie die Strategie abonniert. Den Zeitrahmen wählen, der dem in MetaTrader verwendeten Chart entspricht. |

Alle Parameter sind als `StrategyParam<T>`-Objekte exponiert, damit sie über die grafische Benutzeroberfläche optimiert oder geändert werden können.

## Implementierungshinweise
- Die rollende Geschichte von vier Eröffnungspreisen wird ohne Sammlungen gepflegt, um den Repository-Richtlinien zu entsprechen.
- Stops werden nicht als separate Orders eingereicht; stattdessen prüft die Logik die Kerzen-Ranges und gibt einen Market-Ausstieg aus, wenn das Stop-Level ausgelöst worden wäre.
- Da StockSharp Positionen asynchron verarbeitet, verlässt die Strategie einen bestehenden Trade, bevor ein neues Einstiegssignal ausgewertet wird. Im Live-Trading entspricht dies der ursprünglichen "Schließen dann Wiederöffnen"-Sequenz und vermeidet gleichzeitig überlappende Orders.
- Die Pip-Größe wird von `Security.PriceStep` abgeleitet. Bei 5- oder 3-stelligen Symbolen wird der Schritt mit zehn multipliziert, damit ein Pip der MetaTrader-Definition entspricht.

## Verwendungstipps
- Die Strategie auf Instrumenten mit konsistenten Tick-Größen ausführen, wo pip-basierte Stops sinnvoll sind (z.B. wichtige Forex-Paare).
- Den `StopLossPips`-Wert pro Instrument optimieren; große Werte erweitern den Schutzpuffer, während kleinere Werte die Strategie empfindlicher für Intrabar-Rauschen machen.
- Sicherstellen, dass die Broker-Verbindung Kerzen-Updates mit endgültigen Zuständen sendet, damit die Strategie die korrekten Eröffnungspreise erhält.

## Risiken und Einschränkungen
- Trades nur eine Bar lang zu halten bedeutet, dass die Strategie stark vom gewählten Zeitrahmen abhängt. Backtesting verschiedener Kerzen-Dauern ist unerlässlich.
- Die Verwendung von Kerzen-Extremen zur Emulation von Stop-Ausführungen führt in volatilen Märkten im Vergleich zu nativen Stop-Orders zu Slippage.
- Die Strategie bleibt nach den ersten vier Datenbars immer im Markt (entweder long oder short), was in Seitwärtsmärkten häufige Trades erzeugen kann.
