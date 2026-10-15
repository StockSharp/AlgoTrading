# Neuro Nirvaman-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Neuro Nirvaman-Strategie ist eine direkte Konvertierung des MetaTrader 5-Expertenberaters *NeuroNirvamanEA*. Sie recreiert den Perzeptron-basierten Entscheidungsbaum aus der ursprünglichen MQL-Implementierung durch die Kombination von vier Laguerre-geglätteten positiven Richtungsindikatoren (+DI) mit zwei SilverTrend-Swing-Detektoren. Die Strategie arbeitet auf abgeschlossenen Kerzen und sendet Market-Orders mit dynamischen Take-Profit- und Stop-Loss-Levels in Punkten. Kein Trailing-Stop, Averaging oder Pyramidisierung wird angewendet – es kann jeweils nur eine einzige Position existieren.

## Markteingaben und Indikatoren
- **AverageDirectionalIndex (4 Instanzen)** – jede Instanz ist mit ihrer eigenen Periode konfiguriert. Die Strategie liest die +DI-Komponente und leitet sie durch einen Laguerre-Filter, um glatte Oszillatorwerte im Bereich `[0, 1]` zu erhalten.
- **LaguerrePlusDiState** – ein interner Helfer, der die Logik des benutzerdefinierten Indikators `laguerre_plusdi.mq5` reproduziert, einschließlich der vierstufigen Laguerre-Glättung und `CU / (CU + CD)`-Normalisierung.
- **SilverTrendState (2 Instanzen)** – ein treuer Port der `silvertrend_signal.mq5`-Logik. Bewertet die letzten 10 Kerzen (`SSP = 9`), um Ausbruchspunkte zu erkennen, und gibt `1` bei bärischen Pfeilen, `-1` bei bullischen Pfeilen oder `0` aus, wenn kein Pfeil vorhanden ist.
- **Kerzen-Stream** – die Strategie abonniert einen einzigen Zeitrahmen, der über `CandleType` ausgewählt wird, und verarbeitet nur abgeschlossene Kerzen.

## Handelslogik
1. **Signalvorbereitung**
   - Jeder Laguerre-Wert wird über den Helfer `ComputeTensionSignal` in eine diskrete Aktivierung übersetzt: Werte über `0.5 + distance/100` erzeugen `-1`, unter `0.5 - distance/100` erzeugen `1`, und die neutrale Zone produziert `0`.
   - SilverTrend-Swings werden bei jeder Kerze aktualisiert. Die Risikoparameter (`Risk1`, `Risk2`) verringern oder verbreitern den Support/Resistance-Kanal genau wie im MQL-Indikator.
2. **Perzeptrone**
   - **Perzeptron 1** mischt die erste Laguerre-Aktivierung mit dem ersten SilverTrend-Swing unter Verwendung der Gewichte `X11 - 100` und `X12 - 100`.
   - **Perzeptron 2** mischt die zweite Laguerre-Aktivierung mit dem zweiten SilverTrend-Swing unter Verwendung der Gewichte `X21 - 100` und `X22 - 100`.
   - **Perzeptron 3** arbeitet auf der dritten und vierten Laguerre-Aktivierung mit den Gewichten `X31 - 100` und `X32 - 100`.
3. **Supervisor (Pass-Parameter)**
   - `Pass = 3`: erfordert `Perzeptron 3 > 0`. Wenn auch `Perzeptron 2 > 0`, kauft die Strategie mit `TakeProfit2` / `StopLoss2`. Andernfalls, wenn `Perzeptron 1 < 0`, wird mit `TakeProfit1` / `StopLoss1` verkauft.
   - `Pass = 2`: wenn `Perzeptron 2 > 0`, wird eine Long-Position mit dem zweiten Risikolimitsatz eröffnet. Wenn `Perzeptron 2 <= 0`, wird ein Short mit dem ersten Limitensatz eröffnet.
   - `Pass = 1`: wenn `Perzeptron 1 < 0`, verkauft die Strategie mit dem ersten Risikoset. Andernfalls geht sie Long mit denselben Risikoeinstellungen.
4. **Ordermanagement**
   - Einstiege werden mit `BuyMarket` oder `SellMarket` ausgeführt und verwenden den Parameter `TradeVolume` als Lot-Größe.
   - Take-Profit- und Stop-Loss-Levels werden vom Schlusskurs der Signalkerze berechnet: `entry ± points * PriceStep`. Sie werden bei jeder abgeschlossenen Kerze durch Hoch/Tief-Prüfungen überwacht und emulieren die ursprünglichen MT5-Schutzorders.
   - Neue Signale werden ignoriert, solange eine Position aktiv ist; nur wenn die Position geschlossen wird, werden neue Trades bewertet.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 15-Minuten-Zeitrahmen | Kerzentyp für Berechnungen. |
| `TradeVolume` | `decimal` | 0.1 | Positionsvolumen in Lots. |
| `Risk1`, `Risk2` | `int` | 3 / 9 | SilverTrend-Risikofaktoren, die die Kanalbreite definieren. |
| `Laguerre1Period` – `Laguerre4Period` | `int` | 14 | ADX-Länge für jeden Laguerre-Glättungsstream. |
| `Laguerre1Distance` – `Laguerre4Distance` | `decimal` | 0 | Abstand in Prozent (0–100) um den 0.5-Schwellenwert, der die neutrale Zone definiert. |
| `X11`, `X12`, `X21`, `X22`, `X31`, `X32` | `decimal` | 100 | Gewichtskoeffizienten; der MQL-Code subtrahiert 100 vor der Anwendung. |
| `TakeProfit1`, `StopLoss1`, `TakeProfit2`, `StopLoss2` | `int` | 100 / 50 | Schutzabstände in Punkten. |
| `Pass` | `int` | 3 | Supervisor-Modus, der die für den Handel verwendete Perzeptron-Kombination auswählt. |

## Verwendungshinweise
- Standardgewichte (`100`) neutralisieren die Perzeptrone. Um die Strategie zu aktivieren, passen Sie die Gewichte von `100` weg an, damit die Perzeptrone Nicht-Null-Ausgaben erzeugen können.
- Die SilverTrend-Implementierung respektiert die ursprüngliche Alarm-Zähl-Logik (ohne Alarme) und hält den Zustand zwischen Kerzen, sodass Signale mit der MT5-Version übereinstimmen.
- Da Take-Profit- und Stop-Loss-Levels intern simuliert werden, wird das Hoch/Tief jeder abgeschlossenen Kerze verwendet, um Ziel-Treffer zu prüfen. Intrabar-Spitzen zwischen Ticks werden nicht modelliert.
- Die Strategie ist einzelsymbolisch und verwaltet keine mehrfachen Instrumente. Hängen Sie sie an das gewünschte Wertpapier an und konfigurieren Sie die Kerzenserie entsprechend.
- Es sind jeweils nur Long- oder Short-Positionen erlaubt; das Umkehren der Position erzwingt zuerst einen vollständigen Ausstieg.

## Bereitstellung
1. Die Lösung bauen und die Strategie aus dem StockSharp-Samples-Launcher oder in einem benutzerdefinierten Projekt ausführen.
2. Das Wertpapier auswählen, die Kerzenserie zuweisen und die Perzeptron-Gewichte sowie Risikoparameter konfigurieren.
3. Die Strategie starten und Trades mit dem automatisch erstellten Diagramm überwachen (Laguerre-Indikatoren und eigene Deals werden dem Bereich hinzugefügt).
4. Optimierungen können über die integrierten Parameter-Metadaten (`SetCanOptimize`) ausgeführt werden, falls gewünscht.
