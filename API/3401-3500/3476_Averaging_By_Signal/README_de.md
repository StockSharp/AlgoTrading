# Mittelung nach Signalstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Averaging By Signal Strategy** portiert den MetaTrader-Experten `AveragingBySignal.mq4` zum StockSharp-High-Level-API. Der ursprüngliche Berater kombinierte einen Crossover-Einstiegsfilter mit gleitendem Durchschnitt mit einer Durchschnittsbildung im Martingale-Stil, einem gemeinsamen Korb-Take-Profit und einem optionalen Trailing-Stop, der nur bei der allerersten Order aktiviert wird. Diese C#-Version erstellt dieselben Bausteine ​​neu und passt sie gleichzeitig an das Netting-Ausführungsmodell und das Indikator-Framework von StockSharp an.

## Handelslogik
1. Abonnieren Sie den konfigurierten Zeitrahmen (`CandleType`) und geben Sie zwei gleitende Durchschnitte ein, die mit den angeforderten Zeiträumen und Methoden erstellt wurden (`FastPeriod`/`FastMethod`, `SlowPeriod`/`SlowMethod`).
2. Warten Sie, bis die Kerzen vollständig geschlossen sind. Wenn ein Balken abgeschlossen ist, vergleichen Sie die vorherigen und aktuellen Werte beider Durchschnittswerte, um einen schnellen/langsamen Übergang zu erkennen.
3. Signale erzeugen:
   - ein zinsbullischer Crossover (schneller Anstieg über langsam) ergibt ein Long-Signal;
   - ein rückläufiger Crossover (schneller Rückgang unter langsam) ergibt ein Short-Signal;
   - andernfalls bleibt die Strategie untätig.
4. Bei einem neuen Long-Signal und solange kein Long-Basket aktiv ist, übermitteln Sie eine Marktkauforder unter Verwendung des vom Positionsgrößenblock zurückgegebenen Basisvolumens.
5. Bei einem neuen Short-Signal und solange kein Short-Korb aktiv ist, erteilen Sie einen Marktverkaufsauftrag.
6. Mittelungsregeln:
   - Der Abstand zur nächsten Ebene wird durch `LayerDistancePips`, konvertiert in Pips im MetaTrader-Stil, gesteuert.
   - Zusätzliche Long-Layer erfordern entweder ein bullisches Signal (wenn `AveragingBySignal` wahr ist) oder nur die Preisbedingung (wenn falsch);
   - zusätzliche kurze Schichten folgen der symmetrischen Logik;
   - Die Losgröße jeder neuen Ebene wird mit dem `LotSizing`-Modus berechnet und auf `MaxLayers` Einträge pro Richtung begrenzt.
7. Warenkorbverwaltung:
   - Jeder ausgeführte Trade wird im FIFO verfolgt, um den durchschnittlichen Einstiegspreis der Long- und Short-Körbe zu rekonstruieren.
   - Der gewichtete Durchschnittspreis plus/minus `TakeProfitPips` bildet den gemeinsamen Take-Profit. Wenn der Schlusskurs dieses Niveau erreicht, wird der gesamte Korb geschlossen;
   - Wenn `EnableTrailing` aktiviert ist und genau eine Order in einem Warenkorb vorhanden ist, wird nach `TrailingStartPips` des variablen Gewinns ein Trailing Stop aktiviert. Der Stop wird immer dann vorgezogen, wenn sich der Preis um mindestens `TrailingStepPips` verbessert.
8. Die Strategie funktioniert in einer Netting-Umgebung: Gegensignale gleichen das bestehende Risiko automatisch aus, bevor der nächste Korb geöffnet wird.

## Positionsgrößenbestimmung und Pip-Berechnung
- `InitialVolume` definiert das Basislos. Wenn `LotSizing` auf `Multiplier` gesetzt ist, multipliziert jede zusätzliche Schicht das Basislos mit `Multiplier^layerIndex` und reproduziert so die Logik von MQL `LotType`.
- Der Helfer passt das angeforderte Volumen an die `VolumeStep`, `MinVolume` und `MaxVolume` des Instruments an, sodass jede Bestellung börsenkonform ist.
- Pip-Werte werden von `Security.PriceStep` abgeleitet und ahmen die ursprüngliche „zweistellige“ Anpassung nach: Fünfstellige FX-Symbole verwenden 0,0001, während vierstellige Symbole unverändert 0,0001 verwenden.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-stündiger Zeitrahmen | Primärer Zeitrahmen für Indikatorberechnungen. |
| `InitialVolume` | `decimal` | `0.1` | Basislosgröße für die erste Bestellung in einem Warenkorb. |
| `LotSizing` | `LotSizingMode` | `Multiplier` | Wählen Sie zwischen festen Losgrößen oder geometrischer Skalierung. |
| `Multiplier` | `decimal` | `2` | Der Losmultiplikator wird auf jede zusätzliche Ebene angewendet, wenn `LotSizing` = `Multiplier`. |
| `FastPeriod` | `int` | `28` | Rückblick auf den schnellen gleitenden Durchschnitt. |
| `FastMethod` | `MovingAverageMethod` | `LinearWeighted` | Methode des gleitenden Durchschnitts für die schnelle Linie. |
| `SlowPeriod` | `int` | `50` | Rückblick auf den langsam gleitenden Durchschnitt. |
| `SlowMethod` | `MovingAverageMethod` | `Smoothed` | Methode des gleitenden Durchschnitts für die langsame Linie. |
| `TakeProfitPips` | `int` | `15` | Gemeinsame Take-Profit-Distanz für den gesamten Korb (0 deaktiviert). |
| `AveragingBySignal` | `bool` | `true` | Erfordern Sie ein neues Signal, bevor Sie neue Ebenen hinzufügen. |
| `LayerDistancePips` | `decimal` | `10` | Minimale nachteilige Bewegung (in Pips) vor der Mittelwertbildung. |
| `MaxLayers` | `int` | `10` | Maximale gleichzeitige Bestellungen pro Richtung, einschließlich der ersten. |
| `EnableTrailing` | `bool` | `false` | Aktivieren Sie den Trailing Stop für Einzelbestellungskörbe. |
| `TrailingStartPips` | `decimal` | `10` | Vor Beginn des Trailings ist ein variabler Gewinn erforderlich. |
| `TrailingStepPips` | `decimal` | `1` | Zusätzlicher Fortschritt ist erforderlich, um den Trailing Stop zu verschieben. |

## Unterschiede zum ursprünglichen Fachberater
- StockSharp arbeitet im Netting-Modus, während MetaTrader 4 unabhängige Absicherungspositionen zulässt. Wenn ein Signal die Richtung ändert, gleicht die neue Marktorder das bestehende Risiko aus, bevor ein neuer Korb erstellt wird.
- Der geteilte Take-Profit wird als expliziter Exit-Befehl implementiert, anstatt jedes Ticket mit `OrderModify` zu ändern.
- Der Trailing Stop wird mit Marktaustritten modelliert, die durch Kerzenschlusskurse ausgelöst werden. Der ursprüngliche Experte stützte sich auf Stop-Updates auf Tick-Ebene; Daher liegt die C#-Version möglicherweise etwas später zurück, folgt aber denselben Schwellenwerten.
- Risikoprüfungen wie `AccountFreeMarginCheck` und Slippage-Handling entfallen, da StockSharp-Broker die Margen-/Preisregeln direkt durchsetzen.

## Anwendungstipps
- Stellen Sie genaue Instrumentenmetadaten (`PriceStep`, `VolumeStep`, minimale und maximale Lautstärke) für korrekte Pip- und Volumenkonvertierungen bereit.
- Halten Sie `FastPeriod` unbedingt niedriger als `SlowPeriod`; Die Strategie stoppt automatisch, wenn die Konfiguration gültige Crossovers verhindern würde.
- Deaktivieren Sie `AveragingBySignal`, wenn Sie ein reines Raster wünschen, das unabhängig vom letzten Crossover ausschließlich auf Preisniveaus reagiert.
- Da die Exit-Logik bei geschlossenen Kerzen funktioniert, führen kürzere Zeitrahmen zu schnelleren Reaktionen, können aber auch das Rauschen und die Anzahl der Mittelungsschichten erhöhen.
