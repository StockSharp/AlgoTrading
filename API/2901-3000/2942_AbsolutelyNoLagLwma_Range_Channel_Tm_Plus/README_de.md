# AbsolutelyNoLagLWMA Kanalbereich TM Plus Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein direkter Port des MetaTrader-Experten "Exp_AbsolutelyNoLagLwma_Range_Channel_Tm_Plus". Sie handelt einen Preiskanal, der aus einem doppelt geglätteten linearen gewichteten gleitenden Durchschnitt (LWMA) der Kerzenhochs und -tiefs abgeleitet wird. Die StockSharp-Version behält das ursprüngliche Verhalten bei: Signale werden auf abgeschlossenen Kerzen eines auswählbaren Zeitrahmens ausgewertet, der Kanalzustand wird auf dieselbe Weise wie der MQL-Indikator kodiert, und das Positionsmanagement folgt derselben Prioritätsreihenfolge (Zeitausstieg zuerst, Indikatorausstiege zweite, neue Einstiege zuletzt).

## Indikatoraufbau
1. Für jede abgeschlossene Kerze werden die Hoch- und Tiefreihen in einen ersten LWMA eingespeist. Der Längenparameter wird zwischen den Hoch- und Tiefströmen geteilt.
2. Der Ausgang des ersten LWMA wird erneut mit einem weiteren LWMA gleicher Länge geglättet. Dies recreiert die "AbsolutelyNoLagLWMA"-Glättung des ursprünglichen Indikators.
3. Die endgültigen oberen und unteren Kanalwerte werden mit dem Kerzenschlusskurs verglichen:
   * Schluss über der oberen Linie → bullischer Ausbruchszustand.
   * Schluss unter der unteren Linie → bärischer Ausbruchszustand.
   * Schluss innerhalb des Kanals → neutraler Zustand.
4. Die Strategie speichert die jüngsten Kanalzustände. Der Parameter `SignalBar` steuert, welcher Balkenindex für die Signalgenerierung geprüft wird (0 = letzte geschlossene Kerze, 1 = ein Balken zurück usw.), entsprechend der `SignalBar`-Eingabe des MQL-Programms.

## Signalinterpretation
* **Long-Einstieg** – aktiviert durch `EnableBuyEntries`. Die Strategie sucht nach einem bullischen Ausbruch auf dem durch `SignalBar + 1` indizierten Balken, während der Balken bei `SignalBar` bereits in den Kanal zurückgekehrt ist. Das Verhalten repliziert den ursprünglichen "vorheriger Balken-Ausbruch"-Test.
* **Short-Einstieg** – aktiviert durch `EnableSellEntries`. Spiegelt die Long-Logik für bärische Ausbrüche.
* **Long-Ausstieg** – aktiviert durch `EnableBuyExits`. Ein bärischer Ausbruch auf dem Referenzbalken schließt bestehende Long-Positionen, sofern sie nicht bereits durch den zeitbasierten Ausstieg auf der aktuellen Kerze geschlossen wurden.
* **Short-Ausstieg** – aktiviert durch `EnableSellExits`. Ein bullischer Ausbruch auf dem Referenzbalken schließt offene Shorts, sofern der zeitbasierte Ausstieg das Schließen noch nicht angefordert hat.

## Trade-Management
* **Auftragsvolumen** – entnommen aus dem Parameter `OrderVolume`. Umkehrorders addieren automatisch den Absolutwert der aktuellen Position, um Restexposure zu vermeiden.
* **Stop Loss / Take Profit** – optionale absolute Offsets definiert in Instrumentenpunkten (`StopLossPoints`, `TakeProfitPoints`). Wenn positiv, werden sie in Preisoffsets unter Verwendung des Instruments `PriceStep` umgerechnet und an `StartProtection` übergeben.
* **Zeitbasierter Ausstieg** – der ursprüngliche EA schließt Positionen, die eine Haltezeit überschreiten (`TimeTrade`, `nTime`). In StockSharp wird dies durch `UseTimeExit` und `HoldingLimit` gehandhabt. Der Ausstieg wird vor Indikatorsignalen auf jeder abgeschlossenen Kerze ausgewertet.
* **Positionszeitplanung** – die Strategie zeichnet den Zeitstempel des letzten Trades auf, der zu einer Long- oder Short-Position führte. Diese Zeitstempel werden für den zeitbasierten Ausstieg verwendet.

## Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| `Length` | Länge beider LWMA-Durchläufe, die den Kanal formen. |
| `SignalBar` | Verschiebung des für Signale untersuchten Balkens (0 = letzte geschlossene Kerze). |
| `CandleType` | Zeitrahmen für Indikator und Trade-Auswertung. |
| `OrderVolume` | Volumen bei Einreichung neuer Eingangsorders. |
| `StopLossPoints` | Stop-Loss-Distanz in Instrumentenpunkten (0 deaktiviert den Stop). |
| `TakeProfitPoints` | Take-Profit-Distanz in Instrumentenpunkten (0 deaktiviert das Ziel). |
| `EnableBuyEntries` | Neue Long-Positionen erlauben oder verbieten. |
| `EnableSellEntries` | Neue Short-Positionen erlauben oder verbieten. |
| `EnableBuyExits` | Dem Indikator erlauben, Long-Positionen zu schließen. |
| `EnableSellExits` | Dem Indikator erlauben, Short-Positionen zu schließen. |
| `UseTimeExit` | Schließen von Positionen nach Ablauf von `HoldingLimit` aktivieren. |
| `HoldingLimit` | Maximale Haltezeit, bevor der Zeitausstieg ausgelöst wird. |

## Hinweise
* Der Kanal wird aus Kerzenhochs und -tiefs genau wie der beigefügte MQL-Indikator `AbsolutelyNoLagLwma_Range_Channel` berechnet.
* Die Strategie ignoriert unvollständige Kerzen und arbeitet nur mit abgeschlossenen Daten, um vorzeitige Signale zu vermeiden.
* `SignalBar` auf `0` zu setzen entspricht der typischen MT5-Konfiguration, bei der die letzte geschlossene Kerze analysiert wird. Höhere Werte reproduzieren die verzögerte Bestätigung des Standard-EA (`SignalBar = 1`).
* Wenn `PriceStep` für das ausgewählte Instrument nicht verfügbar ist, werden die Stop-Loss- und Take-Profit-Offsets ignoriert, was das Verhalten von Null-Eingaben im ursprünglichen Skript beibehält.
