# 21-Stunden-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **21-Stunden**-Strategie reproduziert das Verhalten des MQL4-Expertenberaters `21hour.mq4`. Es arbeitet in einem täglichen Zeitfenster: Ausstehende Breakout-Orders werden zu einer konfigurierbaren Startzeit erstellt und alle Risiken werden zu einer konfigurierbaren Stoppzeit entfernt. Die StockSharp-Version behält die gleiche „zwei Stop-Orders um den Preis herum“-Idee bei und nutzt gleichzeitig das High-Level-API für die Auftragsverwaltung, Marktdatenabonnements und die schützende Take-Profit-Abwicklung.

## Handelslogik
- Zu Beginn jedes Handelstages, wenn die Serverzeit mit `StartHour:00` übereinstimmt, liest die Strategie die neuesten Geld-/Briefkurse und platziert sowohl eine Kauf-Stopp- als auch eine Verkaufs-Stopp-Order.
  - Der Abstand vom aktuellen Briefkurs zum Kaufstopp-Trigger beträgt `StepPoints * PriceStep`.
  - Der Abstand vom aktuellen Geldkurs zum Verkaufsstopp-Auslöser liegt um den gleichen Betrag unter dem Markt.
  - `TakeProfitPoints` wird über die Preisstufe des Instruments in eine Preisdistanz umgewandelt und an `StartProtection` übergeben, sodass sowohl Long- als auch Short-Positionen direkt nach der Ausführung einen schützenden Take-Profit erhalten.
- Pro Tag ist nur eine ausstehende Einrichtung zulässig. Wenn nur eine der beiden Stop-Orders aktiv bleibt (z. B. nachdem eine Seite ausgeführt wurde), storniert die Strategie die verbleibende Pending-Order, um die ursprüngliche EA-Logik widerzuspiegeln.
- Wenn die Uhr `StopHour:00` erreicht, schließt die Strategie alle offenen Positionen am Markt und storniert alle ausstehenden ausstehenden Aufträge. Dies gilt auch dann, wenn kein Ausbruch stattgefunden hat.
- Der Standardkerzenstrom besteht aus einminütigen Daten. Es dient lediglich dazu, die stündlichen Überprüfungen fertiger Kerzen auszulösen, was den `prevtime`-Schutz aus der MQL-Version nachahmt.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `Volume` | Auftragsvolumen in Lots für beide ausstehenden Aufträge. | `0.1` |
| `StartHour` | Stunde (0–23), in der das Paar ausstehender Aufträge erstellt wird. | `10` |
| `StopHour` | Stunde (0–23), in der die Strategie Positionen schließt und alle ausstehenden Aufträge entfernt. | `22` |
| `StepPoints` | Abstand in Instrumentenpunkten zwischen dem aktuellen Geld-/Briefkurs und jedem Stop-Eintrag. Durch Multiplikation mit `PriceStep` in den Preis umgerechnet. | `15` |
| `TakeProfitPoints` | Abstand in Punkten vom Einstiegspreis zum Take-Profit-Ziel, verwaltet von `StartProtection`. Auf `0` setzen, um das Ziel zu deaktivieren. | `200` |
| `CandleType` | Kerzendatentyp, der für die Zeiterfassung verwendet wird. Der Standardwert ist ein einminütiger Zeitrahmen (`TimeSpan.FromMinutes(1).TimeFrame()`). | `1 minute` |

## Implementierungshinweise
- Verwendet `SubscribeCandles`, um fertige Kerzen zu erhalten und den Stundenplan nur einmal pro Minute auszuwerten.
- Abonniert Level-1-Kurse über `SubscribeLevel1()`, um die neuesten Geld-/Briefwerte für eine genaue Stop-Platzierung beizubehalten.
- Verlässt sich auf `StartProtection` mit einer Take-Profit-Einheit, um den Take-Profit für ausstehende Aufträge vom ursprünglichen EA zu emulieren, anstatt Aufträge manuell anzuhängen.
- Verfolgt die aktiven Kauf- und Verkaufs-Stop-Orders und ruft `CancelOrder` auf, wenn nur noch eine Seite übrig ist, um sicherzustellen, dass das System nie mit einer ungepaarten ausstehenden Order läuft.
- Ruft `BuyMarket` / `SellMarket`-Helfer auf, um Positionen zur Stoppstunde zu glätten, wobei ausschließlich die übergeordnete Strategie API verwendet wird.

## Verhaltenshinweise
- Die Strategie erwartet, dass die Brokerverbindung Preisschrittinformationen bereitstellt. Wenn `PriceStep` fehlt, bleiben die Preise ungerundet.
- Ausstehende Bestellungen werden nur einmal pro Kalendertag generiert. Sie werden am nächsten Handelstag zur konfigurierten Startzeit neu erstellt, auch wenn der Ausbruch am Vortag nicht ausgelöst wurde.
- Wenn `TakeProfitPoints` Null ist, platziert die Strategie immer noch ausstehende Aufträge, es wird jedoch kein schützender Take-Profit verwaltet.
