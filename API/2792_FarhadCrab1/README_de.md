# FarhadCrab1 Strategie (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die FarhadCrab1-Strategie ist ein Trendfolge-System, das Trades auf Pullbacks zu einem exponentiellen gleitenden Durchschnitt (EMA) eingeht und Ausstiege mit festen Stops, Take-Profits, einem Parabolic SAR-inspirierten Trailing-Stop und einem höheren Zeitrahmen-Filter verwaltet. Der originale MetaTrader 5 Expert Advisor stützt sich auf stündliche Kerzen für die Ausführung, während er auf tägliche Daten verweist, um zu entscheiden, wann offene Positionen geschlossen werden sollen. Dieser C#-Port behält dieselbe Kernlogik bei, indem er einen EMA-Filter des Arbeitszeitrahmens mit einer täglichen EMA-Kreuzungs-Ausstiegsregel kombiniert.

## Kernkonzepte
- **Trendfilter:** Ein EMA, der auf dem Arbeitszeitrahmen berechnet wird (standardmäßig 15-Perioden-EMA auf 1-Stunden-Kerzen). Nur Long-Signale sind erlaubt, wenn das Tief der vorherigen Kerze über dem EMA bleibt, und nur Short-Signale sind erlaubt, wenn das Hoch der vorherigen Kerze unter dem EMA bleibt.
- **Tagesfilter:** Ein separater EMA, der auf täglichen Kerzen berechnet wird. Wenn der tägliche EMA über den täglichen Schlusskurs kreuzt, werden alle Long-Positionen geschlossen. Wenn er darunter kreuzt, werden alle Short-Positionen geschlossen. Dies imitiert die ursprüngliche `ClosePositions`-Logik aus dem MQL5-Code.
- **Risikokontrollen:** Feste Stop-Loss- und Take-Profit-Niveaus werden aus Pip-Abständen abgeleitet. Ein Trailing-Stop verschiebt den Schutz-Stop, sobald die Position genug Gewinn erzielt, und emuliert die MT5-Trailing-Funktion, die `TrailingStop`- und `TrailingStep`-Einstellungen kombiniert.
- **Einzelpositions-Management:** Die Strategie handelt eine einzelne Nettoposition. Einen Long-Einstieg zu tätigen, während ein Short gehalten wird (oder umgekehrt), schließt zuerst die entgegengesetzte Exposure, bevor der neue Trade eröffnet wird.

## Handelsregeln
1. **Signalerkennung (Arbeitszeitrahmen):**
   - Long-Einstieg, wenn das Tief der vorherigen Kerze größer ist als der EMA-Wert (nach Anwendung des konfigurierten Shifts).
   - Short-Einstieg, wenn das Hoch der vorherigen Kerze kleiner ist als der EMA-Wert.
2. **Positionsgrößenbestimmung:** Der `Volume`-Parameter setzt die Basis-Ordergröße. Beim Umkehren von Short zu Long (oder umgekehrt) sendet die Engine automatisch die zusätzlich benötigte Menge, um die Nettoposition umzukehren.
3. **Stop-Loss und Take-Profit:**
   - Abstände werden in Pips definiert. Die Pip-Größe passt sich automatisch an die Tick-Größe des Instruments an, wobei FX-Symbole mit fünf und drei Stellen einen 10x-Multiplikator verwenden, um das MT5-Verhalten zu entsprechen.
   - Stop-Loss oder Take-Profit kann durch Setzen des jeweiligen Pip-Abstands auf null deaktiviert werden.
4. **Trailing-Stop:**
   - Aktiviert sich nur, wenn `TrailingStopPips` größer als null ist.
   - Der Stop wird auf `aktueller_Preis - TrailingStopPips` (für Longs) oder `aktueller_Preis + TrailingStopPips` (für Shorts) verschoben, sobald der Positionsgewinn `TrailingStopPips + TrailingStepPips` überschreitet.
   - Der zusätzliche Trailing-Schritt verhindert häufige Modifikationen.
5. **Tages-Austrittsfilter:**
   - Verwendet die letzten zwei abgeschlossenen Tageskerzen.
   - Long-Positionen werden geschlossen, wenn der tägliche EMA vor zwei Tagen unter dem täglichen Schlusskurs lag und am aktuellsten Tag über dem täglichen Schlusskurs liegt (bärische Kreuzung).
   - Short-Positionen werden geschlossen, wenn die entgegengesetzte Kreuzung auftritt.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-Stunden-Zeitrahmen | Arbeitszeitrahmen für den Ausführungs-EMA und die Einstiegslogik. |
| `MaLength` | `int` | 15 | Periode des EMA auf dem Arbeitszeitrahmen. |
| `MaShift` | `int` | 0 | Anzahl der abgeschlossenen Kerzen, die verwendet werden, um den EMA rückwärts zu verschieben. |
| `DailyMaLength` | `int` | 15 | Periode des täglichen EMA, der den Kreuzungs-Austrittsfilter liefert. |
| `StopLossPips` | `decimal` | 50 | Stop-Loss-Abstand in Pips. Auf `0` setzen zum Deaktivieren. |
| `TakeProfitPips` | `decimal` | 50 | Take-Profit-Abstand in Pips. Auf `0` setzen zum Deaktivieren. |
| `TrailingStopPips` | `decimal` | 10 | Trailing-Stop-Abstand in Pips. Auf `0` setzen zum Deaktivieren des Trailings. |
| `TrailingStepPips` | `decimal` | 5 | Minimaler zusätzlicher Gewinn in Pips, bevor der Trailing-Stop wieder aktualisiert wird. |
| `Volume` | `decimal` | 0.1 | Basis-Handelsgröße in Lots/Kontrakten. |

## Hinweise und Unterschiede zur MQL-Version
- Dieser Port verwendet immer exponentielle gleitende Durchschnitte, entsprechend dem ursprünglichen Standard (`MODE_EMA`). Andere MT5-Glättungsmodi werden nicht unterstützt.
- Der MT5 Expert Advisor arbeitet mit Geld-/Briefkursen bei jedem Tick. Diese Übersetzung operiert auf abgeschlossenen Kerzen, sodass Stop-Loss- und Take-Profit-Prüfungen auf Kerzenhochs/-tiefs ausgewertet werden.
- Der Parabolic SAR-Indikator, der in der Originaldatei vorhanden war, beeinflusste keine Handelsentscheidungen und ist daher aus der C#-Implementierung ausgelassen.
- Die Trailing-Logik passt das gespeicherte Stop-Niveau an, sendet aber keine Broker-Stop-Orders. Der Ausstieg erfolgt, wenn der Kerzenbereich das berechnete Stop- oder Take-Profit-Niveau berührt.

## Verwendungstipps
- Einen Kerzentyp wählen, der dem gewünschten Handelshorizont entspricht. Die standardmäßigen Ein-Stunden-Kerzen replizieren das Verhalten des Quellskripts.
- `MaLength` und `DailyMaLength` zusammen anpassen, um die Reaktionsfähigkeit zwischen Intraday-Einstiegen und höheren Zeitrahmen-Trendfiltern abzustimmen.
- Für FX-Symbole, die mit fünf Stellen notiert werden (z.B. EURUSD), werden Pip-Abstände automatisch skaliert, sodass 1 Pip 0.0001 entspricht.
- Beim Backtesting sicherstellen, dass der tägliche Datenstrom verfügbar ist, damit der Austrittsfilter korrekt funktionieren kann.
