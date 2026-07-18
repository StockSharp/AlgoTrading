# Harami CCI Bestätigung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Harami CCI-Bestätigung ist eine hochrangige StockSharp-Portierung des MetaTrader 5-Expertenberaters `Expert_ABH_BH_CCI`. Der ursprüngliche EA handelt mit den Umkehrmustern Bullish Harami und Bearish Harami mit zwei Kerzen. Vor dem Eingehen eines Handels wird eine Bestätigung von einem Commodity Channel Index (CCI)-Oszillator angefordert und die Körpergröße der Kerze anhand eines gleitenden Durchschnitts gemessen, um sicherzustellen, dass die größere Kerze die Spanne wirklich dominiert. Die StockSharp-Konvertierung behält die gleiche Bestätigungslogik bei, verarbeitet nur abgeschlossene Kerzen und nutzt das integrierte Schutzmodul der Plattform zur Bestellsicherheit.

## Strategielogik
### Mustererkennung
* **Berechnung des durchschnittlichen Körpers** – behält einen gleitenden Durchschnitt der absoluten Kerzenkörper über die letzten *N* Balken bei (Standard 5). Dies spiegelt die Hilfsklasse MetaTrader wider, die die Kerzengröße und Trendreferenz glättet.
* **Bullish Harami** – erfordert, dass die vorherige Kerze bullisch ist, dass die vorherige Kerze bärisch ist und einen Körper hat, der länger als der Durchschnitt ist, und dass der bullische Körper innerhalb des bärischen Bereichs bleibt. Der Mittelpunkt der früheren Kerze muss ebenfalls unter dem gleitenden Durchschnitt der Schlusskurse liegen, was einen Abwärtstrend bestätigt.
* **Bearish Harami** – gespiegelte Bedingungen: Die vorherige Kerze muss bärisch sein, die frühere Kerze muss bullisch und long sein, der bärische Körper muss innerhalb der bullischen Spanne liegen und der Mittelpunkt muss über dem nahen gleitenden Durchschnitt liegen, um einen Aufwärtstrend zu bestätigen.

### CCI Bestätigung
* **Eingabefilter** – die Strategie prüft den CCI-Wert der zuletzt abgeschlossenen Kerze (Schicht 1). Für Long-Trades muss der CCI unter `-EntryThreshold` liegen (Standardwert 50), während für Short-Trades ein Wert über `+EntryThreshold` erforderlich ist.
* **Ausgangsband** – der CCI-Verlauf wird auf Überschreitungen von ±`ExitBand` überwacht (Standard 80). Wenn der Indikator über `-ExitBand` steigt, wird jede offene Short-Position geschlossen. Wenn es unter `+ExitBand` fällt, wird die bestehende Langzeitbelichtung geschlossen. Dies reproduziert die „Stimmen“, die der MetaTrader-Experte verwendet, um Positionen abzuflachen.

### Handelsmanagement
* **Umkehrungen** – wenn das entgegengesetzte Harami-Setup bestätigt wird, während die Strategie bereits eine Position hält, wird sie genug Volumen handeln, um sowohl das bestehende Risiko zu schließen als auch die neue Richtung zu eröffnen.
* **Schutz** – `StartProtection()` ist aktiviert, sodass Benutzer bei Bedarf Stop-Loss- oder Take-Profit-Einstellungen über die StockSharp-Benutzeroberfläche hinzufügen können. Standardmäßig werden keine festen Stopps erzwungen, um mit der Quelle EA in Einklang zu bleiben, die auf manuellen Geldverwaltungseinstellungen beruhte.

## Parameter
* **Auftragsvolumen** – Basisvolumen, das bei jedem Markteintritt gesendet wird. Bei einer Umkehrung wird automatisch zusätzliches Volumen hinzugefügt, um die Gegenposition zu schließen.
* **CCI Periode** – Länge des Commodity Channel Index-Oszillators.
* **Körperdurchschnitt** – Anzahl der historischen Kerzen, die bei der Mittelung der Körpergrößen und Schlusskurse verwendet werden.
* **CCI-Eintrag** – minimaler absoluter CCI-Wert, der erforderlich ist, um ein Harami-Signal zu akzeptieren.
* **CCI Exit Band** – Bandgröße, die die CCI Crossover-Exit-Regeln definiert.
* **Kerzentyp** – für Kerzen verwendeter Zeitrahmen (Standard: 1-Stunden-Zeitrahmen).

## Zusätzliche Hinweise
* Alle Berechnungen werden auf abgeschlossenen Kerzen ausgeführt, die von `SubscribeCandles` bereitgestellt werden. Intrabar-Signale werden absichtlich ignoriert, um dem MetaTrader-Ausführungsmodell zu entsprechen.
* Die Strategie behält einen kurzen gleitenden Verlauf von Kerzen und CCI-Werten bei, um die Harami-Regeln auszuwerten, ohne volle Indikatorpuffer neu zu erstellen.
* In diesem Ordner wird nur die C#-Implementierung bereitgestellt. Für diese Konvertierung gibt es keine Python-Version.
