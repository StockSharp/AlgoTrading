# Lawinenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Avalanche-Strategie ist ein gitterartiges Mean-Reversion-System, das vom ursprünglichen Expertenberater MetaTrader Avalanche v1.2 inspiriert wurde. Die Idee besteht darin, die Beziehung zwischen dem Preis und einem Gleichgewichtsreferenzpreis (ERP) in einem höheren Zeitrahmen zu überwachen, der als einfacher gleitender Durchschnitt berechnet wird. Wenn der Preis unter dem ERP liegt, erwartet die Strategie eine Erholung in Richtung des Durchschnitts und baut Long-Positionen auf. Wenn der Preis über dem ERP liegt, sucht die Strategie nach einem Rückgang und baut Short-Positionen auf. Jede zusätzliche Position ist durch konfigurierbare Abstandsschwellen voneinander getrennt, während jeder Eintrag individuelle Stop-Loss- und Take-Profit-Level erhält.

Dieser StockSharp-Port konzentriert sich auf den „in Richtung“-Zweig des ursprünglichen Algorithmus. Absicherungsaufträge außerhalb des ERP-Systems aus der MQL-Version werden nicht repliziert, da die StockSharp-Strategien auf einer einzigen Nettoposition basieren, aber die Grid-Stacking-, Puffer- und Gewinnmitnahmelogik bleibt dem ursprünglichen Ansatz treu.

## Wie es funktioniert

1. Abonnieren Sie zwei Kerzenserien: den Handelszeitrahmen und einen ERP-Zeitrahmen, der den gleitenden Durchschnitt speist.
2. Berechnen Sie einen einfachen gleitenden ERP-Durchschnitt und bestimmen Sie, ob der Preis darüber oder darunter liegt. Ein konfigurierbarer Puffer verhindert häufiges Umdrehen.
3. Wenn eine neue ERP-Verzerrung auftritt, schließen Sie alle offenen Gitter und warten Sie auf neue Signale.
4. Eröffnen Sie eine Anfangsposition in der Richtung, die den Preis wieder in Richtung ERP bringen soll (Long unten, Short oben), wenn das Flag `OpenStartingOrders` aktiviert ist.
5. Fügen Sie weiterhin Positionen in die gleiche Richtung hinzu, wenn der Preis um die Distanz `IntervalToward` steigt (Momentum-Stacking).
6. Fügen Sie zusätzliche Schutzeinträge hinzu, wenn sich der Preis um `IntervalToward + StackBufferToward` gegen das Raster bewegt (Martingal-Stacking).
7. Jeder Eintrag verfügt über ein eigenes Stop-Loss- und Take-Profit-Ziel, das in Punkten gemessen wird. Dadurch wird sichergestellt, dass profitable Abschnitte einzeln geschlossen werden können, während das Raster weiterhin das verbleibende Risiko verwaltet.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `BaseVolume` | Basisauftragsvolumen vor Anwendung von Multiplikatoren. |
| `TowardMultiplier` | Lot-Multiplikator für Standard-ERP-Einträge. |
| `TowardInterestMultiplier` | Multiplikator, der verwendet wird, wenn das Instrument einen positiven Swap in Handelsrichtung zahlt. |
| `IntervalToward` | Distanz in Punkten, die erforderlich ist, um einen trendfolgenden Stapel hinzuzufügen. |
| `StackBufferToward` | Beim Stapeln gegen ungünstige Preisbewegungen wird dem Intervall zusätzlicher Puffer hinzugefügt. |
| `TakeProfitToward` | Take-Profit-Distanz in Punkten für jeden Eintrag. Zum Deaktivieren auf `0` setzen. |
| `StopLossToward` | Stop-Loss-Distanz in Punkten für jeden Eintrag. Zum Deaktivieren auf `0` setzen. |
| `ErpPeriod` | Anzahl der Perioden für den einfachen gleitenden ERP-Durchschnitt. |
| `ErpChangeBuffer` | Puffer (in Punkten), der um das ERP herum angewendet wird, bevor der Bias umgeschaltet wird. |
| `CandleType` | Handelszeitrahmen, der zum Auslösen von Ein- und Ausstiegen verwendet wird. |
| `ErpCandleType` | Zeitrahmen, der zur Berechnung des gleitenden ERP-Durchschnitts verwendet wird. |
| `OpenStartingOrders` | Wenn diese Option aktiviert ist, wird sofort die erste Rasterbestellung geöffnet, wenn die Bedingungen erfüllt sind. |

## Unterschiede zum Original EA

- Da die StockSharp-Strategie eine einzige Nettoposition beibehält, wird nur der Richtung-ERP-Zweig implementiert. Absicherungsaufträge entfallen.
- Die Auftragsausführung basiert auf Marktaufträgen und nicht auf den ausstehenden Stop-Aufträgen, die in der MQL-Version verwendet werden.
- Die Erkennung der Swap-Richtung bleibt erhalten, um zwischen dem Standard- und dem Zinsmultiplikator zu wählen.

## Anwendungstipps

- Passen Sie `IntervalToward` und `StackBufferToward` an, um zu steuern, wie aggressiv das Raster neue Trades hinzufügt.
- Stellen Sie sicher, dass das ausgewählte Instrument und die Zeitrahmen ausreichend Liquidität bieten; Gittersysteme können eine beträchtliche Belastung ansammeln.
- Kombinieren Sie die Strategie mit externen Risikokontrollen (Aktienstopps, Sitzungsfilter), wenn Sie sie in der Produktion ausführen.
