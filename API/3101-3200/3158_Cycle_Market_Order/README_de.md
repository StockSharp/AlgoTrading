# Cycle Market Order-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert vom MetaTrader 4 Expert Advisor "CycleMarketOrder_V181". Die Strategie organisiert eine feste Anzahl von Slots innerhalb einer Preisskala und eröffnet Marktorders, wenn das aktuelle Bid/Ask durch einen einzelnen Slot handelt. Jeder Slot trägt sein eigenes Volumen, Break-Even-Schwellenwert und Trailing-Stop-Wert, sodass das Grid schrittweise in eine Position skalieren kann, während bereits erreichte Gewinne geschützt werden.

## Handelslogik

1. Die Pip-Größe wird aus dem Instrument-Kursschritt und der Dezimalgenauigkeit abgeleitet (5/3-stellige Symbole entsprechen 10 Punkten pro Pip). Die Parameter `MaxPrice`, `SpanPips` und `MaxCount` werden dann verwendet, um den Preisbereich jedes Slots vorzuberechnen.
2. Level-1-Marktdaten werden konsumiert, um das Tick-basierte Verhalten des ursprünglichen Expert Advisors nachzubilden. Jedes Update aktualisiert die zwischengespeicherten besten Bid/Ask-Preise.
3. Wenn `UseWeekendMode` aktiviert ist, verweigert die Strategie den Handel außerhalb des konfigurierten Wochenend-Fensters (Samstag ab `WeekendHour`, den ganzen Sonntag und Montag vor `WeekstartHour`).
4. Bei Long-Zyklen (`EntryDirection = 1`) scannt der Algorithmus Slots vom niedrigsten zum höchsten Bezeichner. Wenn der aktuelle Ask-Preis zwischen `startPrice` und `endPrice` des Slots fällt, wird eine Markt-Kauforder mit `OrderVolume` gesendet. Short-Zyklen (`EntryDirection = -1`) spiegeln diese Logik und verwenden den Bid-Preis.
5. Slot-Zustände verfolgen ausstehende Ein-/Ausstiegsorders, gefülltes Volumen und den durchschnittlichen Einstiegspreis. Logging verwendet `MagicNumberBase + index` um die MT4-"Magic"-Bezeichner zu entsprechen.
6. Trailing-Verwaltung wird bei jedem Level-1-Update vor der Auswertung neuer Einträge ausgeführt. Sobald der Gewinn bei einem Long-Slot `BreakEvenPips + TrailingStopPips` überschreitet, wird der Stop auf `Bid - TrailingStopPips` gesetzt. Short-Slots verwenden `Ask + TrailingStopPips` und die gespiegelte Break-Even-Bedingung. Wenn der Marktpreis den gespeicherten Stop kreuzt, wird der Slot mit einer Marktorder geschlossen.
7. Da nur Marktorders verwendet werden, gibt es keine ausstehenden Orders zu stornieren. Teilfüllungen passen das verbleibende Slot-Volumen an, sodass die Strategie weiterhin traillen oder den Slot neu bewaffnen kann, sobald er flach wird.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `EntryDirection` | Handelsrichtung: `1` kauft die Leiter, `-1` verkauft sie, `0` deaktiviert neue Einträge, hält Trailing aktiv. |
| `MaxPrice` | Oberer Ankerkurs zur Berechnung der Slot-Bereiche. |
| `MaxCount` | Gesamtanzahl aktiver Slots im Grid. |
| `SpanPips` | Abstand in Pips zwischen aufeinanderfolgenden Slot-Grenzen. |
| `OrderVolume` | Volumen das gesendet wird wenn ein Slot auslöst. |
| `BreakEvenPips` | Gewinnabstand der überschritten werden muss bevor der Trailing Stop bewaffnet wird. |
| `TrailingStopPips` | Trailing-Abstand der nach Erreichen des Break-Even angewendet wird. |
| `UseWeekendMode` | Aktiviert das Wochenend-Handelssperrfenster. |
| `WeekendHour` | Stunde am Samstag (Terminalzeit) wenn der Handel ausgesetzt wird. |
| `WeekstartHour` | Stunde am Montag wenn der Handel wieder aufgenommen wird. |
| `MagicNumberBase` | Bezeichner-Offset in Log-Nachrichten um die Original-Magic-Numbers zu entsprechen. |

## Implementierungshinweise

* Die Slot-Verwaltung verfolgt ausstehende Ein- und Ausstiegsorders, sodass wiederholte Füllungen kein doppeltes Volumen registrieren.
* Die Strategie setzt ihren Trailing Stop zurück wann immer eine neue Füllung das Slot-Engagement erhöht und stellt sicher, dass der Stop den zuletzt gemittelten Einstiegspreis widerspiegelt.
* Der Wochenendschutz überspringt einfach sowohl Trailing- als auch Einstiegslogik; bestehende Positionen bleiben während des Sperrzeitraums unberührt.
* Level-1-Daten sind erforderlich weil die Logik rohe Bid/Ask-Preise statt Kerzenschlusskurse vergleicht und damit das Tick-für-Tick-Verhalten der MT4-Version eng reproduziert.
