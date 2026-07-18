# Hans123 Trader Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Hans123 Trader ist ein Ausbruchssystem, das aus dem originalen MetaTrader 5 Expert Advisor *Hans123_Trader* konvertiert wurde. Die Strategie scannt einen rollierenden Preisbereich und platziert ausstehende Stop-Orders während eines konfigurierbaren Intraday-Fensters. Schutz-Stops, Gewinnziele und Trailing-Regeln spiegeln die MQL5-Logik wider, sodass der StockSharp-Port sich wie der Quell-Roboter verhält.

## Kernkonzepte
- **Bereichsausbruch** – verwendet das höchste Hoch und das niedrigste Tief der letzten *N* Kerzen, um den Ausbruchskanal zu definieren.
- **Zeitfilter** – wertet Signale nur zwischen den Start- und Endstunden aus, um Nachtlärm zu vermeiden.
- **Synchrone ausstehende Orders** – aktualisiert Buy-Stop- und Sell-Stop-Orders bei jeder abgeschlossenen Kerze innerhalb des Handelsfensters.
- **Risikokontrolle** – optionale Stop-Loss-, Take-Profit- und Trailing-Stop-Abstände in Pips ausgedrückt.
- **Dynamisches Trailing** – sobald der Preis die Trailing-Stop- plus Trailing-Step-Distanz zurücklegt, wird der Schutz-Stop enger gezogen, um Gewinne zu sichern.

## Handelslogik
1. Die ausgewählte Kerzenserie abonnieren und warten, bis sich das `RangeLength`-Indikator-Fenster gebildet hat.
2. Bei jeder abgeschlossenen Kerze:
   - Den 80-Bar (konfigurierbar) Hoch/Tief-Kanal aktualisieren.
   - Verarbeitung überspringen, wenn die aktuelle Zeit außerhalb des Intervalls `[StartHour, EndHour)` liegt.
   - Bestehende Einstiegsorders stornieren und neue Stop-Orders platzieren:
     - **Buy Stop** am Bereichshoch für `OrderVolume`.
     - **Sell Stop** am Bereichstief für `OrderVolume`.
3. Wenn eine Einstiegsorder ausgeführt wird:
   - Die entgegengesetzte ausstehende Order stornieren.
   - Stop-Loss- und Take-Profit-Orders registrieren, wenn die entsprechenden Pip-Abstände größer als null sind.
4. Während eine Position offen ist:
   - Wenn der Preis mindestens `TrailingStopPips + TrailingStepPips` vorrückt, den Schutz-Stop um `TrailingStopPips` in Richtung Markt verschieben.
   - Schutzorders werden automatisch storniert, wenn die Position auf null zurückgeht.

## Parameter
| Name | Beschreibung | Standardwert |
| --- | --- | --- |
| `OrderVolume` | Ordergröße für Ausbruchseinstiege. | `0.1` |
| `RangeLength` | Anzahl der Kerzen im Ausbruchskanal. | `80` |
| `StopLossPips` | Stop-Loss-Abstand in Pips (0 deaktiviert den Stop). | `50` |
| `TakeProfitPips` | Take-Profit-Abstand in Pips (0 deaktiviert das Ziel). | `50` |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips (0 deaktiviert Trailing). | `10` |
| `TrailingStepPips` | Zusätzliche Pips, die benötigt werden, bevor der Trailing-Stop aktualisiert wird. Muss positiv sein, wenn Trailing aktiviert ist. | `5` |
| `StartHour` | Inklusiver Stundenwert des Tages (UTC), wenn Ausbruchsorders beginnen. | `6` |
| `EndHour` | Exklusiver Stundenwert des Tages (UTC), wenn Ausbruchsorders enden. | `10` |
| `CandleType` | Arbeitender Kerzen-Datentyp und Zeitrahmen. | `1 Stunde` Kerzen |

## Praktische Hinweise
- Die Pip-Größe passt sich den Dezimalstellen des Wertpapiers an (3/5-stellige Forex-Symbole erhalten die übliche *×10*-Anpassung).
- Trailing-Stops werden erst erstellt, nachdem eine Position die Aktivierungsdistanz zurückgelegt hat; wenn `StopLossPips` null ist, wird der anfängliche Stop weggelassen, bis die Trailing-Bedingungen erfüllt sind.
- Portfolio-Berechtigungen an das ausgewählte `OrderVolume` und die Kontraktgröße des Instruments anpassen.
- Die StockSharp-Konvertierung verwendet Diagramm-Hilfsmittel zur Visualisierung von Kerzen, dem Kanal und Trades zum Debuggen.

## Unterschiede zur MQL5-Version
- Stop- und Ziel-Orders werden über StockSharp-High-Level-Helfer registriert, anstatt über MetaTrader-Handelsanfragen.
- Volumen-Standardwerte bleiben identisch (0.1 Lots), können aber über `StrategyParam`-Metadaten optimiert werden.
- Ausstehende Orders werden bei jeder abgeschlossenen Kerze aktualisiert, anstatt auf Tick-Level-Updates zu warten, was dem StockSharp-Ereignismodell entspricht.

## Verwendung
1. Die Strategie an ein Portfolio/Wertpapier-Paar anhängen und sicherstellen, dass das Kerzenabonnement mit dem gewünschten Zeitrahmen übereinstimmt.
2. Parameter für die Instrumentvolatilität und Sessiongrenzen anpassen.
3. Die Strategie starten; die Diagrammbereichsüberlagerung überwachen, um Ausbruchsniveaus und ausgeführte Trades zu bestätigen.
4. Die integrierten Parameter für die Optimierung innerhalb der StockSharp-Testumgebung verwenden, falls gewünscht.
