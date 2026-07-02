# Breadandbutter2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Breadandbutter2-Strategie ist eine direkte Umsetzung des MT4 Expert Advisors von `MQL/7710/Breadandbutter2.mq4`. Das System überwacht einstündige Kerzen und verfolgt drei linear gewichtete gleitende Durchschnitte (LWMA), die auf den Eröffnungspreisen der Kerzen basieren. Ein synchronisierter Schnittpunkt der drei Durchschnittswerte deutet auf eine Trendumkehr hin. Die Strategie dreht die Position sofort um, um sie an die neue Richtung anzupassen, und verteilt optional zusätzliche Aufträge, während der Trend anhält.

## Kernlogik
1. Abonnieren Sie einstündige Kerzen (konfigurierbar über **Kerzentyp**).
2. Berechnen Sie LWMA(5), LWMA(10) und LWMA(15) bei Kerzeneröffnungen.
3. Erkennen Sie eine zinsbullische Umkehr, wenn die vorherige Kerze `LWMA5 < LWMA10 < LWMA15` hatte und die aktuelle Kerze `LWMA5 > LWMA10 > LWMA15` anzeigt. Erkennen Sie eine rückläufige Umkehr mit der entgegengesetzten Ungleichungssequenz.
4. Bei einem bullischen Crossover sollten Sie eine Long-Position mit **Volumen**-Lots anstreben. Zielen Sie bei einem bearischen Crossover auf eine gleich große Short-Position. Die Strategie passt die bestehende Position an, indem sie nur die Differenz zwischen dem aktuellen und dem Zielengagement kauft oder verkauft.
5. Nach jeder Eingabe wird der **Intervallzähler** zurückgesetzt. Sobald die fertigen Kerzen im **Intervall** ohne einen neuen Crossover vergehen, fügt die Strategie eine weitere Order in der aktuellen Richtung hinzu (Pyramidenbildung) und aktualisiert die Schutzorder.
6. Gewinnziel und Verlustlimit werden jeder resultierenden Position mithilfe der in Preisschritten ausgedrückten **Take-Profit**- und **Stop-Loss**-Abstände zugeordnet. Wenn Sie einen der beiden Werte auf Null setzen, wird der entsprechende Schutz deaktiviert.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| **Volumen** | 0,1 | Bestellgröße in Losen für jeden Basiseingang und jede Pyramidenschicht. |
| **Gewinn mitnehmen** | 20 | Abstand in Preisschritten für die Take-Profit-Order. Zum Deaktivieren auf 0 setzen. |
| **Stop-Loss** | 20 | Abstand in Preisschritten für den Schutzstopp. Zum Deaktivieren auf 0 setzen. |
| **Intervall** | 4 | Anzahl der fertigen Kerzen, die gewartet werden müssen, bevor eine weitere Pyramidenposition hinzugefügt wird. Null deaktiviert die Pyramidenbildung. |
| **Kreuzfilter** | 1.1 | Reservierter Parameter, der aus dem Originalcode für zukünftige ADX-Filterung übernommen wurde (derzeit nicht verwendet). |
| **Kerzentyp** | 1-stündiger Zeitrahmen | Kerzendatenquelle für die LWMA-Berechnungen. |

## Positionsmanagement
- Die Hilfsmethode `AdjustPosition` stellt sicher, dass die endgültige Position nach jedem Crossover genau mit der gewünschten Belichtung übereinstimmt.
- Pyramiding-Trades basieren auf dem aktuellen Vorzeichen von `Position`, um nur Lots in der bestehenden Richtung hinzuzufügen.
- `SetTakeProfit` und `SetStopLoss` werden nach jedem Trade aufgerufen, um die Risikokontrollen mit der neuesten Positionsgröße synchron zu halten.

## Notizen
- Das MT4-Skript hat einen ADX-Wert berechnet, ihn aber nie verwendet; Der Parameter **Kreuzfilter** wird aus Kompatibilitätsgründen und für zukünftige Erweiterungen beibehalten.
- Bei der ursprünglichen MQL-Implementierung war der Intervallzähler auskommentiert. Die StockSharp-Version aktiviert das beabsichtigte Pyramidenverhalten durch das Zählen fertiger Kerzen.
- `StartProtection()` wird während `OnStarted` aufgerufen, um integrierte Positionsschutzdienste zu aktivieren.
