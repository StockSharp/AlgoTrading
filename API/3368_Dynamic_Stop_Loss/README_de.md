# Dynamischer Stop-Loss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Der ursprüngliche MetaTrader-Expertenberater „Dynamic Stop Loss“ eröffnet selbst keine neuen Trades. Stattdessen beobachtet es bestehende Marktpositionen und positioniert den schützenden Stop-Loss neu, sobald eine neue Kerze erscheint, sodass er in einem festen Abstand zum aktuellen Preis bleibt. Der StockSharp-Port behält das gleiche Verhalten bei: Jeder abgeschlossene Balken löst eine Neuberechnung des Schutzstopps für die Seite aus, die gerade geöffnet ist. Wenn keine Position vorhanden ist, bleibt die Strategie einfach im Leerlauf, bis eine neue Position erkannt wird.

## Wie es funktioniert
1. Die Strategie abonniert Kerzen, die durch den Parameter `Candle Type` definiert sind (Standardzeitrahmen 1 Minute).
2. Wenn eine Kerze schließt, wird der Schlusskurs mit dem vom Benutzer ausgewählten Punktabstand multipliziert. Die Distanz wird von Punkten im MetaTrader-Stil über `Security.PriceStep` in ein absolutes Preisdelta umgewandelt (Fallback auf `Security.Step`, dann auf `1`).
3. Wenn eine Long-Position offen ist, storniert die Strategie alle bestehenden Stop-Orders und setzt einen neuen Verkaufs-Stopp bei `Close - Distance`.
4. Wenn eine Short-Position offen ist, wird der Stop mithilfe einer Kauf-Stopp-Order auf `Close + Distance` verschoben.
5. Wenn die Position geschlossen wird (manuell oder durch Stop-Filling), wird die Trailing-Order gelöscht, um veraltete Schutz-Orders zu vermeiden.

Dies führt zu derselben ständig neu verankerten Stop-Distanz wie bei der MQL-Version, was bedeutet, dass sich der Stop bei Schwankungen der Kerzen sowohl näher an den Markt als auch weiter davon entfernen kann.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `StopLossPoints` | `800` | Abstand zwischen dem Marktpreis und dem Schutzstopp, gemessen in Instrumentenpunkten. Der Wert wird mit `Security.PriceStep` multipliziert (Fallback auf `Security.Step`, dann `1`), bevor er auf den Schlusskurs angewendet wird. Auf `0` setzen, um die Stoppverwaltung zu deaktivieren. |
| `CandleType` | `TimeFrameCandle(00:01:00)` | Kerzentyp, der definiert, wann der Stop neu berechnet wird. Wählen Sie einen Zeitrahmen, der dem in MetaTrader verwendeten Diagramm entspricht. |

## Nutzungshinweise
- Die Strategie geht davon aus, dass Trades durch externe Strategien, manuelle Vorgänge oder andere Komponenten eröffnet werden. Es verwaltet nur den Stop-Loss.
- Stellen Sie sicher, dass die Sicherheitsmetadaten (`PriceStep`, `Step`, Volumen) gefüllt sind, damit die Point-to-Price-Konvertierung mit der Tick-Größe des Brokers übereinstimmt. Instrumente, die mit gebrochenen Pips notiert werden, müssen den richtigen Schritt anzeigen.
- Da der Stop bei jedem Kerzenschluss neu berechnet wird, folgt er dem Preis, selbst wenn sich der Markt gegen die Position bewegt. Dies spiegelt die MetaTrader-Logik wider, bei der `OrderModify` immer die neuesten `Bid`/`Ask` minus/plus der konfigurierten Entfernung verwendet.
- Die erstellten Stop-Orders ersetzen immer die vorherigen, um die Plattform mit dem neuesten Schutzniveau synchron zu halten.
