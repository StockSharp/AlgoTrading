# Strategie Donchian Scalper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Donchian Scalper ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `DonchianScalperEA`. Die Strategie überwacht Donchian Kanalgrenzen und den exponentiellen gleitenden Durchschnitt (EMA) derselben Länge. Eine ausstehende Stop-Order wird erst aktiviert, wenn der Preis wieder unter EMA fällt, was signalisiert, dass die Dynamik vor einem möglichen Ausbruch zurückgesetzt wurde. Einträge werden mit Stop-Orders ausgeführt, die an den aktuellen Donchian-Extremen platziert und durch das entgegengesetzte Band geschützt sind. Gewinne werden entweder durch eine feste Take-Profit-Distanz oder durch adaptive Trailing-Stops gesteuert, die die gewählte Marktstruktur verfolgen.

## Strategielogik
### Einreisevorbereitung
* **Pullback-Validierung** – die Strategie wartet, bis eine der beiden zuvor geschlossenen Kerzen unter EMA (für Long-Positionen) oder über EMA (für Short-Positionen) fällt. Die Kreuzungsebene wird um den konfigurierbaren *Cross Anchor*-Abstand versetzt, um sicherzustellen, dass der Rückzug sinnvoll ist.
* **Breakout-Aktivierung** – Sobald die Pullback-Bedingung erfüllt ist und der Cooldown-Timer abgelaufen ist, wird eine Stop-Order an der aktuellsten Donchian-Grenze übermittelt (oberes Band für Long-Positionen, unteres Band für Short-Positionen). Das gegenüberliegende Band definiert den anfänglichen Schutzstopp. Bestehende ausstehende Aufträge werden automatisch neu ausgerichtet, wenn die Donchian-Niveaus für mindestens zwei Kerzen abflachen.

### Handelsmanagement
* **Anfänglicher Schutz** – Wenn eine Breakout-Order ausgeführt wird, platziert die Strategie eine schützende Stop-Order unter Verwendung des vorberechneten Stop-Preises. Das Stop-Level entspricht dem gegenüberliegenden Donchian-Band und kann durch die Einstellung *Stop Loss (Punkte)* nach innen verschoben werden.
* **Gewinnkontrolle** – zwei Verwaltungsmodi stehen zur Verfügung:
  * *Close At Profit* – schließt die Position, sobald die Nettobewegung vom durchschnittlichen Einstiegspreis die konfigurierte Take-Profit-Distanz überschreitet.
  * *Trailing* – hält den Handel offen und verschärft regelmäßig den Schutzstopp. Die nachfolgende Engine kann der Donchian-Grenze, der EMA oder einem ATR-basierten Volatilitätsband folgen.
* **Abklingzeit** – nachdem alle Positionen geschlossen sind, wartet die Strategie auf die angegebene Anzahl abgeschlossener Kerzen, bevor sie neue Breakout-Orders aktiviert. Dies reproduziert die MetaTrader-Logik, die mindestens drei Balken zwischen den Trades erfordert.

## Parameter
* **Volumen** – Ordervolumen, das für Stop-Einstiege und Marktausstiege verwendet wird.
* **Kanalzeitraum** – Donchian Kanallänge, wird auch für den EMA-Filter verwendet.
* **Kreuzanker** – zusätzliche Distanz (in Punkten), die der Rückzug überschreiten muss, bevor der Ausbruchsbefehl aktiviert wird.
* **Stop-Loss (Punkte)** – Distanz, die zum gegenüberliegenden Donchian-Band für den anfänglichen Schutzstopp addiert wird; auf `0` setzen, um den Anschlag direkt auf dem Band zu platzieren.
* **Take Profit (Punkte)** – Gewinnziel, das vom Modus *Close At Profit* verwendet wird. Wird ignoriert, wenn der Trailing-Modus aktiv ist.
* **Kerzentyp** – Berechnungen von Zeitrahmen-Fahrindikatoren.
* **Profit-Modus** – wählt zwischen dem festen Take-Profit-Exit und adaptiven Trailing-Stops.
* **Trailing-Modus** – Trailing-Engine, die im *Trailing*-Gewinnmodus verwendet wird. Zur Auswahl stehen Donchian-Grenze, EMA oder ATR-basiertes Nachstellen.
* **Abklingzeitbalken** – Mindestanzahl fertiger Kerzen, die vergehen müssen, nachdem die Position flach geworden ist, bevor neue Aufträge erteilt werden können.
* **ATR-Periode / ATR-Multiplikator** – Parameter für die ATR-Trailing-Engine. Der Multiplikator definiert, wie viele ATRs subtrahiert (Long) oder addiert (Short) werden, um den Trailing Stop zu berechnen.

## Zusätzliche Hinweise
* Die Strategie richtet jeden Stop- und Einstiegspreis an der Preisstufe des Instruments aus, um die Börsenkonformität sicherzustellen.
* Wenn sowohl Long- als auch Short-Stop-Orders aktiv sind, wird durch die Ausführung einer Seite automatisch die gegenüberliegende Pending-Order storniert, um eine Absicherung zu vermeiden.
* Wenn *Take Profit (Punkte)* auf Null gesetzt ist, während der Gewinnmodus bei *Close At Profit* bleibt, hält die Strategie die Positionen offen, bis der Schutzstopp erreicht wird.
* Die Konvertierung konzentriert sich auf die übergeordnete StockSharp API: Indikatorbindung, Kerzenabonnements und Hilfsmethoden (`BuyStop`, `SellStop`, `SellMarket` usw.). Die Python-Implementierung ist in diesem Paket nicht enthalten.
