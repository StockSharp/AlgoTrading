# Executor AO Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Executor AO ist eine Saucer-Strategie für den Awesome Oscillator, die ursprünglich als "Executor AO" MetaTrader-Experte verteilt wurde.
Der StockSharp-Port behält die indikatorbasierte Umkehrerkennung bei und vereinfacht das Geldmanagement auf eine feste Ordergröße.
Die Strategie beobachtet abgeschlossene Kerzen des konfigurierten Zeitrahmens, wertet die Steigungsänderung des Awesome Oscillators aus
und öffnet eine einzelne Nettoposition, wenn bullische oder bärische Bedingungen unter oder über der Nulllinie auftreten. Der optionale
Schutz-Stop, Take-Profit und die Trailing-Logik reproduzieren das Risikomanagementverhalten des Original-EA.

## Handelslogik
1. Die Kerzenserie, die durch `CandleType` definiert ist, abonnieren und jede fertige Kerze mit den konfigurierten Parametern
   `AoShortPeriod` und `AoLongPeriod` in den Awesome Oscillator einspeisen.
2. Die letzten drei abgeschlossenen Awesome-Oscillator-Werte speichern, um das MetaTrader-Pufferzugriffsmuster des Original-Experten
   zu reproduzieren.
3. Wenn keine Position offen ist:
   - **Bullische Aufstellung**: Der neueste AO-Wert ist größer als der vorherige, der vorherige Wert ist kleiner als der Wert vor
     zwei Balken (ein Tal), und der neueste Wert bleibt unter `-MinimumAoIndent`. In diesem Fall eine Markt-Kauforder mit
     `TradeVolume` Lots senden.
   - **Bärische Aufstellung**: Der neueste AO-Wert ist kleiner als der vorherige, der vorherige Wert ist größer als der Wert vor
     zwei Balken (ein Gipfel), und der neueste Wert bleibt über `MinimumAoIndent`. In diesem Fall eine Markt-Verkaufsorder mit dem
     festen Volumen einreichen.
4. Wenn eine Position besteht, emuliert die Strategie die Ausstiege des EA:
   - Stop-Loss- und Take-Profit-Preise vom Einstieg aus berechnen, indem `StopLossPips` und `TakeProfitPips` mit der angepassten
     Pip-Größe multipliziert werden (MetaTraders 3/5-Stellenbehandlung wird repliziert).
   - Die Trailing-Stop-Regel anwenden, wenn sich der Preis zugunsten der Position um mehr als `TrailingStopPips +
     TrailingStepPips` Pips bewegt. Der Stop wird nur vorgerückt, wenn das neue Niveau über dem vorherigen liegt, entsprechend
     der Trailing-Step-Anforderung des EA.
   - Long-Positionen schließen, wenn der Preis den Take-Profit oder Stop-Loss berührt oder wenn der Awesome-Oscillator-Wert des
     vorherigen Balkens positiv wird. Short-Positionen schließen, wenn der Preis ihre Ziele trifft oder der vorherige AO-Wert unter
     null fällt.
5. Alle Orders sind Marktorders; das Nettoppositionsmodell von StockSharp stellt sicher, dass nur eine Richtung gleichzeitig aktiv ist.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 5-Minuten-Kerzen | Primärer Zeitrahmen zur Berechnung und zum Handel der Strategie. |
| `TradeVolume` | `decimal` | `1` | Feste Ordergröße für jeden Einstieg. |
| `AoShortPeriod` | `int` | `5` | Schnelle Periode für den kurzen SMA des Awesome Oscillators. |
| `AoLongPeriod` | `int` | `34` | Langsame Periode für den langen SMA des Awesome Oscillators. |
| `MinimumAoIndent` | `decimal` | `0.001` | Mindestabstand von null für neue Signale. Verhindert Trades, wenn AO nahe null schwebt. |
| `StopLossPips` | `decimal` | `50` | Schutz-Stop-Loss-Abstand in MetaTrader-Pips. Auf `0` setzen, um den Stop zu deaktivieren. |
| `TakeProfitPips` | `decimal` | `50` | Take-Profit-Abstand in Pips. Auf `0` setzen, um das Ziel zu deaktivieren. |
| `TrailingStopPips` | `decimal` | `5` | Trailing-Stop-Aktivierungsabstand. Wird nur verwendet, wenn größer als null. |
| `TrailingStepPips` | `decimal` | `5` | Mindest-Preisverbesserung vor Aktualisierung des Trailing Stops. Muss positiv bleiben, wenn Trailing aktiviert ist. |

## Unterschiede zum MetaTrader-EA
- Die MetaTrader-Version erlaubte risikobasierte Positionsgrößenbestimmung. Der StockSharp-Port implementiert die Festlot-Option
  (`TradeVolume`) und lässt das Prozentrisikomanagement aus Gründen der Klarheit weg.
- Das Ordermanagement wird innerhalb der Strategie simuliert: Wenn Stop-Loss- oder Take-Profit-Schwellen bei abgeschlossenen Kerzen
  erreicht werden, sendet die Strategie Marktorders, um die Position zu schließen. Dies spiegelt das EA-Verhalten ohne separate
  Kind-Orders wider.
- Trailing-Anpassungen erfolgen bei Kerzenschluss-Ereignissen statt bei jedem Tick. Dies hält die Implementierung konsistent mit
  der High-Level-API bei gleicher Schwellenlogik.
- Alle Code-Pfade verwenden das `SubscribeCandles` + `Bind`-Muster von StockSharp statt manuell Indikatorpuffer zu kopieren.

## Verwendungstipps
- `TradeVolume` vor dem Start der Strategie am Lot-Schritt des Instruments ausrichten. Der Konstruktor weist denselben Wert auch
  `Strategy.Volume` zu, sodass Hilfsmethoden automatisch die gewählte Größe verwenden.
- `MinimumAoIndent` kann auf rauschenden Märkten erhöht werden, um häufige Wechsel nahe null zu vermeiden. Es auf `0` zu setzen
  reproduziert das aggressivste Verhalten des EA.
- Beim Aktivieren des Trailing Stops `TrailingStepPips` über null halten; andernfalls wirft der Konstruktor eine Exception und
  reproduziert damit die Parametervalidierung des Original-EA.
- Die Strategie an ein Diagramm anhängen, um sowohl Kerzen als auch den Awesome-Oscillator-Overlay zu visualisieren. Dies hilft
  bei der Validierung der Tal-/Gipfelerkennung nach der Konvertierung.

## Indikator
- **Awesome Oscillator**: Differenz zwischen einem schnellen und einem langsamen einfachen gleitenden Durchschnitt des Medianpreises.
  Die Standard-5/34-Konfiguration entspricht dem MetaTrader-Indikator.
