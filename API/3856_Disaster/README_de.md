# Katastrophenstrategie (MQL #7704)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Der ursprüngliche MetaTrader-Expertenberater namens `disaster.mq4` rüstet Stop-Orders rund um einen sehr langen einfachen gleitenden Durchschnitt (SMA) aus. Es wartet, bis sich der aktuelle Preis weit genug vom Durchschnitt entfernt, und parkt dann zwei ausstehende Stop-Orders, die versuchen, eine Rückkehr zur Mean-Reversion zu erzielen. Jede neue Minute berechnet den SMA neu und verschiebt die ausstehenden Bestellungen auf den neuesten Basiswert. Ausgeführte Aufträge werden durch einen festen Stop-Loss und einen adaptiven Take-Profit geschützt, der schrumpft, nachdem der vorherige Handel auf derselben Seite mit einem Verlust geschlossen wurde.

## Notizen portieren

* **Datenquelle** – das MQL-Skript verwendet 1-Minuten-Balken bis `iMA(PERIOD_M1, 590)`. Die StockSharp-Version abonniert eine konfigurierbare Kerzenserie (Standard `TimeSpan.FromMinutes(1)`) und speist einen `SMA`-Indikator mit demselben Lookback.
* **Trigger-Logik** – MetaTrader vergleicht die Geld-/Briefkurse mit SMA und erfordert eine Lücke von 20 Pip, bevor eine ausstehende Order aktiviert wird. Der C#-Port reproduziert dies, indem er den Parameter `TriggerDistancePips` mithilfe des Instruments `PriceStep`/`MinPriceStep` plus dem 10-fachen Multiplikator für 3/5-stellige FX-Symbole in einen absoluten Preisabstand umwandelt.
* **Ordertypen** – der EA registriert Stop-Orders über `OrderSend(..., OP_BUYSTOP/OP_SELLSTOP, ...)`. StockSharp-Äquivalente sind `BuyStop` und `SellStop`. Der Port hält beide Befehle unabhängig und lässt beide Befehle aktiv bleiben, wenn die Bedingungen weiterhin bestehen.
* **Dynamische Verschiebung** – immer wenn eine neue Kerze eintrifft, ruft der MQL-Code `OrderModify` auf, sodass die ausstehenden Stopps die neue SMA verfolgen. StockSharp erreicht dasselbe, indem es `ReRegisterOrder` aufruft, um aktive Bestellungen zu verschieben, ohne die Abwanderung zu stornieren/neu zu erstellen.
* **Stoppniveaus** – MetaTrader erzwingt Broker-Stoppniveaus (`MODE_STOPLEVEL`). Die StockSharp-Version respektiert indirekt dieselbe Sicherheitsmarge, indem sie auf die Instrumentenpreisstufe rundet und die Verschiebung abbricht, wenn der berechnete Preis ungültig ist (≤ 0).
* **Schutzaufträge** – in MT4 werden Stop-Loss und Take-Profit an den ausstehenden Auftrag angehängt. StockSharp erstellt unmittelbar nach einer Einstiegsausführung separate Stop/Limit-Schutzaufträge, die die genauen Preisversätze widerspiegeln.
* **Adaptiver Take-Profit** – der EA halbiert die Take-Profit-Distanz für die nächste Order, wenn der vorherige Trade auf dieser Seite Geld verloren hat. Der Hafen behält die Flaggen `_lastBuyWasLoss` / `_lastSellWasLoss` bei und passt die Take-Profit-Distanz entsprechend an.
* **Geldmanagement** – das Skript beziffert die Lots mit `0.4 * AccountFreeMargin / 1000`, begrenzt durch Broker-Limits. Der StockSharp-Port stellt einen direkten `Volume`-Parameter bereit und richtet ihn an `VolumeStep`, `MinVolume` und `MaxVolume` aus.

## Parameter

| Parameter | Standard | Beschreibung |
| --- | --- | --- |
| `Volume` | `0.1` | Auftragsvolumen abgestimmt auf den Instrumentenvolumenschritt. |
| `MaPeriod` | `590` | Einfache gleitende Durchschnittslänge, die als Basislinie verwendet wird. |
| `StopLossPips` | `30` | Abstand zwischen dem Einstiegspreis und dem Schutzstopp. |
| `TakeProfitPips` | `70` | Basis-Take-Profit-Distanz. Automatische Halbierung nach einem Verlusttrade auf derselben Seite. |
| `TriggerDistancePips` | `20` | Erforderliche Lücke zwischen Preis und SMA, bevor Stop-Einträge aktiviert werden. |
| `CandleType` | `1-minute time frame` | Kerzenreihe zur Versorgung des SMA. |

Alle Pip-basierten Parameter werden über das Instrument `PriceStep` oder `MinPriceStep` übersetzt. Bei FX-Paaren mit 3 oder 5 Dezimalstellen multipliziert die Konvertierung den Schritt mit 10, was dem Verhalten von MetaTrader `Point` entspricht.

## Arbeitsablauf

1. Abonnieren Sie Level1-Kurse und Minutenkerzen.
2. Aktualisieren Sie gespeicherte Geld-/Briefpreise für jede Level1-Nachricht.
3. Berechnen Sie bei jeder fertigen Kerze den SMA neu und verschieben Sie alle aktiven ausstehenden Aufträge auf die neue Basislinie.
4. Wenn keine Position offen ist und die Geld-/Brieflücke den Triggerabstand überschreitet, platzieren Sie die entsprechende Stop-Order (verkaufen Sie über SMA, kaufen Sie darunter, wenn der Preis unterbewertet ist).
5. Wenn eine Stop-Order ausgeführt wird, registrieren Sie sofort Stop-Loss- und Take-Profit-Orders in den gewünschten Abständen. Behalten Sie das letzte Handelsergebnis im Auge, um den nächsten Take-Profit anzupassen.
6. Stornieren Sie alle ausstehenden/Schutzaufträge, wenn die Strategie endet.

## Unterschiede zur MQL-Version

* Der Port basiert auf StockSharp-Schutzaufträgen anstelle von mit dem Broker verbundenen SL/TP-Feldern. Das Verhalten ist gleichwertig, verwendet jedoch explizite Befehle im Konto.
* MetaTrader erzwingt den Haltestellenabstand mit `MODE_STOPLEVEL`. StockSharp erfüllt diese Anforderung, indem es auf die verfügbare Preisstufe rundet und Aktualisierungen überspringt, wenn der berechnete Preis ungültig ist. In der Praxis sollten dieselben Einschränkungen eingehalten werden, sobald der Adapter die Bestellpreise validiert.
* Der Originalcode berechnet das Handelsvolumen bei jedem Tick aus der freien Marge neu. Der Port StockSharp überlässt die Größenbestimmung dem Benutzer über den Parameter `Volume`, um Klarheit und vorhersehbares Verhalten bei allen Brokern zu gewährleisten.

## Anforderungen

* Instrumente müssen mindestens `PriceStep` oder `MinPriceStep` verfügbar machen. Ohne sie fällt die Pip-zu-Preis-Umrechnung auf `0.0001` zurück, was für wichtige FX-Paare angemessen ist.
* Um FX-Stop-Level-Regeln nachzuahmen, sollte der Datenfeed die besten Bid/Ask-Aktualisierungen (Level 1) liefern. Die Strategie lässt sich elegant verschlechtern, indem der Schlusskurs der Kerze verwendet wird, wenn Quotes fehlen.
* Schutzaufträge erfordern Broker/Börsen, die Stop- und Limit-Aufträge unterstützen. Falls nicht verfügbar, passen Sie den Code an, um auf Marktausgänge zurückzugreifen.

## Anwendungstipps

* Beginnen Sie mit Mikrovolumina (`0.01`) auf Demokonten, um die Preisumrechnungen zu validieren.
* Passen Sie `TriggerDistancePips` und `TakeProfitPips` gemeinsam an: Kleinere Auslöser führen zu häufigeren Trades. Erwägen Sie daher, den Take-Profit entsprechend zu senken.
* Überwachen Sie die Flags `_lastBuyWasLoss` und `_lastSellWasLoss` über Protokolle, um zu bestätigen, dass die adaptive Take-Profit-Logik mit dem MetaTrader-Verlauf übereinstimmt.
