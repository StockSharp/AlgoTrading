# Open Close-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Open Close ist ein Port des MetaTrader 5-Expertenberaters `Open Close.mq5` (Ticket 23090). Die Strategie beobachtet die Beziehung zwischen den Eröffnungs- und Schlusskursen der zwei jüngsten abgeschlossenen Kerzen. Es wird jeweils eine Position gehandelt: Wenn die neuere Kerze sich relativ zur vorherigen umkehrt, wird eingestiegen; wenn beide Kerzen in dieselbe Richtung zeigen, wird ausgestiegen. Die C#-Version reproduziert das originale adaptive Lot-Sizing, das die Exposition nach einer Verlustserie reduziert.

## Strategielogik
### Kerzenmuster-Filter
* Die Strategie arbeitet ausschließlich mit abgeschlossenen Kerzen, die durch den konfigurierbaren `CandleType`-Parameter geliefert werden.
* Sie hält ein rollendes Fenster der zwei letzten abgeschlossenen Kerzen (als `previous` und `older` bezeichnet).
* Das Muster vergleicht sowohl Eröffnungs- als auch Schlusskurse dieser Kerzen:
  * **Bullische Umkehr** – `previous.Open > older.Open` **und** `previous.Close < older.Close`.
  * **Bärische Umkehr** – `previous.Open < older.Open` **und** `previous.Close > older.Close`.

### Einstiegsregeln
* Wenn keine Position offen ist und das bullische Umkehrmuster erscheint, sendet die Strategie eine Markt-Kauforder.
* Wenn keine Position offen ist und das bärische Umkehrmuster erscheint, sendet sie eine Markt-Verkaufsorder.
* Es ist nur eine Position erlaubt. Gegensignale werden ignoriert, bis der aktive Trade geschlossen ist.

### Ausstiegsregeln
* Wenn eine Long-Position gehalten wird, steigt die Strategie aus, wenn sich beide verfolgten Kerzen nach unten bewegen (`previous.Open < older.Open` und `previous.Close < older.Close`).
* Wenn eine Short-Position gehalten wird, ist der Ausstiegstrigger symmetrisch (`previous.Open > older.Open` und `previous.Close > older.Close`).
* Im originalen Berater gibt es keine Stop-Loss- oder Take-Profit-Orders, daher stützt sich der Port ausschließlich auf die Kerzenbeziehung zum Schließen von Trades.

### Positionsgröße und Verlustserien-Handling
* Das Ordervolumen wird primär durch `MaximumRiskPercent` bestimmt – der gewünschte Anteil des Portfoliowerts, der pro Trade investiert wird. Die Rohgröße ist `Portfolio.CurrentValue × MaximumRiskPercent ÷ referencePrice` unter Verwendung des letzten Schlusskurses als Preisersatz.
* Wenn die Portfoliobewertung oder der Preis nicht verfügbar ist, dient der `FallbackVolume`-Parameter als sicherer Standardwert.
* Nach jedem vollständig geschlossenen Trade wird der realisierte PnL gespeichert. Die aufeinanderfolgende Verlusserie wird über die letzten `HistoryDays` Tage gezählt.
  * Wenn die Serie größer als ein Trade ist, wird die nächste Ordergröße um `volume × losses ÷ DecreaseFactor` reduziert, was die MT5-Logik nachahmt.
* Das endgültige Volumen respektiert den Volumen-Step des Instruments sowie minimale und maximale Volumengrenzen.

### Zusätzliche Implementierungshinweise
* Die Strategie reagiert nur auf `CandleStates.Finished` und stellt sicher, dass das Muster vollständige Marktdaten verwendet.
* Einstiegs- und Ausstiegsprüfungen erfolgen beim Schlusskurs der neuesten Kerze. In MetaTrader wird die Order zur Eröffnung der nächsten Kerze gesendet; der Unterschied ist für höhere Zeitrahmen vernachlässigbar, sollte aber für sehr kurze Intervalle beachtet werden.
* Portfolio-Kennzahlen in StockSharp approximieren die Kontoinformationen von MetaTrader. Passen Sie `MaximumRiskPercent` oder `FallbackVolume` an, wenn der Broker unterschiedliche Kontraktmultiplikatoren verwendet.

## Parameter
| Parameter | Typ | Standard | Beschreibung |
|-----------|-----|----------|--------------|
| `MaximumRiskPercent` | `decimal` | `0.02` | Anteil des Portfoliowerts für eine neue Position (0.02 = 2%). |
| `DecreaseFactor` | `decimal` | `3` | Divisor für die Lotgröße nach aufeinanderfolgenden Verlust-Trades. Größere Werte mildern die Reduktion. |
| `HistoryDays` | `int` | `60` | Anzahl der Kalendertage, die beim Zählen der aktuellen Verlustserie gescannt werden. |
| `FallbackVolume` | `decimal` | `0.1` | Ordervolumen, wenn die risikobasierte Berechnung nicht durchgeführt werden kann. |
| `CandleType` | `DataType` | `TimeFrame(15m)` | Kerzenserie, die Eröffnungs-/Schlusskurse für die Signalerstellung liefert. |

## Unterschiede zur MetaTrader-Version
* Kontomargenchecks stützen sich auf StockSharp's `Portfolio.CurrentValue`; MetaTrader verwendete `AccountFreeMargin`. Das Verhalten entspricht der ursprünglichen Risikoregel nur wenn beide Plattformen ähnliche Bewertungen melden.
* Der Trade-Verlauf wird aus den eigenen Ausführungen der Strategie gesammelt anstatt aus der terminweiten Geschichte. Stellen Sie sicher, dass die Strategie lange genug läuft, um Serienstatistiken zu akkumulieren.
* Der Port behält das Einzelpositionsmodell (keine Pyramidisierung) bei und spiegelt die ursprüngliche Abwesenheit von Schutzorders wider. Fügen Sie bei Bedarf extern Stops für die Risikokontrolle hinzu.
