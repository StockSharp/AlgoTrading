# Ausstehende Profilrasterstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Pending Tread Grid Strategy** ist eine originalgetreue StockSharp-Portierung des MetaTrader 4-Expertenberaters `Pending_tread.mq4`. Das Original EA baut ständig zwei Leitern ausstehender Aufträge neu auf: eine Leiter über dem Markt und eine darunter. Jede Leiter kann für die Verwendung von Kauf- oder Verkaufsaufträgen konfiguriert werden, und der Abstand wird in Pips definiert. Die StockSharp-Implementierung reproduziert das gleiche Verhalten durch die übergeordnete API-Implementierung, ohne zusätzliche Indikatoren oder Sammlungen einzuführen.

## Handelslogik
1. **Bid/Ask-gesteuerte Wartung** – die Strategie abonniert Kurse der Stufe 1 (`SubscribeLevel1`) und behält die neuesten Geld- und Briefkurse bei. Jedes Mal, wenn neue Daten eintreffen, läuft die Wartungsroutine (mit konfigurierbarer Drosselung) und vergleicht die vorhandenen ausstehenden Aufträge mit der konfigurierten Rastergröße.
2. **Über der Marktleiter** – abhängig von `AboveMarketSide` platziert der Algorithmus entweder Kauf-Stopp- oder Verkaufslimit-Orders in Schritten von `PipStep` Pips über dem Markt. Jede neue Bestellung erhält ihr eigenes Take-Profit-Level, ausgeglichen um `TakeProfitPips` Pips.
3. **Below-Market-Leiter** – der Parameter `BelowMarketSide` wählt zwischen Kauf-Limit- und Verkaufs-Stop-Orders, die unterhalb des Marktes gestapelt sind. Es gelten der gleiche Pip-Abstand und die gleiche Take-Profit-Logik.
4. **Stop-Level-Wächter** – der Parameter `MinStopDistancePoints` emuliert die Prüfung MetaTrader `MODE_STOPLEVEL`. Aufträge werden übersprungen, wenn der Abstand zwischen dem Preis und dem jeweiligen Geld-/Brief-Anker kleiner als das angegebene Limit ist.
5. **Throttle** – `ThrottleSeconds` spiegelt die ursprüngliche Fünf-Sekunden-Drosselung wider, die `TRADE_CONTEXT_BUSY`-Fehler vermieden hat. In diesem Intervall wird nur ein Wartungszyklus ausgeführt, unabhängig davon, wie viele Ticks eintreffen.

Alle Pip-basierten Eingaben (`PipStep`, `TakeProfitPips`) werden mithilfe der Instrumente `PriceStep` und `Decimals` in absolute Preisoffsets umgewandelt. Bei fünfstelligen Anführungszeichen wird der Schritt automatisch mit zehn multipliziert, um der MetaTrader-Logik „angepasster Punkt“ zu entsprechen.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `OrderVolume` | 0,01 | Volumen, das bei jeder ausstehenden Bestellung verwendet wird. Auf die Lautstärkestufe des Instruments vor der Registrierung gerundet. |
| `PipStep` | 12 | Abstand zwischen aufeinanderfolgenden Aufträgen in der Leiter, ausgedrückt in Pips. |
| `TakeProfitPips` | 10 | Abstand in Pips, der zur Platzierung des Take-Profits für jede ausstehende Order verwendet wird. |
| `OrdersPerSide` | 10 | Maximale Anzahl aktiver Aufträge, die über und unter dem Markt gehalten werden. |
| `AboveMarketSide` | Kaufen | Oberhalb des Marktes verwendeter Ordertyp. `Buy` erstellt Kauf-Stop-Orders, `Sell` erstellt Verkaufs-Limit-Orders. |
| `BelowMarketSide` | Verkaufen | Ordertyp, der unterhalb des Marktes verwendet wird. `Buy` erstellt Kauf-Limit-Orders, `Sell` erstellt Verkaufs-Stop-Orders. |
| `MinStopDistancePoints` | 0 | Minimal zulässiger Abstand (in Rohpunkten) zwischen dem Geld-/Briefkurs und dem ausstehenden Preis. Legen Sie dies bei Bedarf auf den Broker `MODE_STOPLEVEL` fest. |
| `ThrottleSeconds` | 5 | Abkühlzeit zwischen Netzwartungszyklen. |
| `SlippagePoints` | 3 | Aus Gründen der Dokumentationsparität aufbewahrt; StockSharp ausstehende Orders verwenden diesen Wert nicht. |

## Implementierungshinweise
- Verwendet nur die StockSharp High-Level-Helfer (`SubscribeLevel1`, `BuyLimit`, `SellLimit`, `BuyStop`, `SellStop`).
- Die Preise werden durch `Security.ShrinkPrice` normalisiert, sodass der Broker gültige, an Ticks ausgerichtete Werte erhält.
- Das Volumen wird angepasst, um `VolumeStep`, `MinVolume` und `MaxVolume` zu berücksichtigen, bevor jede Bestellung gesendet wird.
- Alle Diagnosemeldungen werden über `AddInfoLog` / `AddWarningLog` weitergeleitet und spiegeln die ausführliche Ausgabe des Skripts MetaTrader wider.
- Auf die Python-Implementierung wird, wie gewünscht, bewusst verzichtet.

## Nutzungstipps
1. Weisen Sie ein liquides Instrument und Portfolio zu und starten Sie dann die Strategie. Ausstehende Leitern werden sofort nach dem ersten Level-1-Update angezeigt.
2. Erhöhen Sie `OrdersPerSide` mit Vorsicht: Jeder weitere Schritt führt zu einer weiteren Live-Pending-Order auf der Brokerseite.
3. Um den ursprünglichen EA genau nachzuahmen, belassen Sie die Standarddrosselung bei fünf Sekunden und konfigurieren Sie `MinStopDistancePoints` mit der Stop-Level-Anforderung des Brokers.
4. Denken Sie daran, dass StockSharp Nettopositionen verwaltet; Wenn entgegengesetzte Leitern gleichzeitig ausgelöst werden, gleichen sich die resultierenden Auffüllungen teilweise gegenseitig aus, anstatt abgesicherte Unterpositionen zu schaffen.
