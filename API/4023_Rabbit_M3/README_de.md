# Kaninchen M3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Rabbit M3 ist eine Portierung des MetaTrader 4 Expert Advisors `RabbitM3` (auch unter dem Namen „Petes Party Trick“ veröffentlicht). Das System wechselt mithilfe eines Paars stündlicher exponentieller gleitender Durchschnitte zwischen Long-Only- und Short-Only-Regimen. Die Momentum-Bestätigung erfolgt durch einen Williams %R-Kreuz in Kombination mit einem CCI-Level-Filter, während ein extrem langer Donchian-Kanal nach Preisausbrüchen Ausschau hält, die die aktuelle Trendverzerrung zunichte machen. Die Positionsgröße kann optional nach großen Gewinnern wachsen, wodurch die im Originalcode enthaltene Losskalierungsregel repliziert wird.

## Strategielogik
### Trendregimefilter
* Wenn der schnelle EMA unter dem langsamen EMA schließt, wird jegliches bestehende Long-Engagement aufgelöst und neue Signale werden auf die Short-Seite beschränkt.
* Wenn der schnelle EMA über dem langsamen EMA schließt, wird jedes bestehende Short-Engagement geschlossen und nur Long-Setups bleiben berechtigt.
* Wenn die EMAs gleich sind, wird das vorherige Regime beibehalten und spiegelt die MetaTrader-Logik wider, die nur bei strikten Ungleichheiten umschaltet.

### Einreisebestimmungen
* **Short-Trades**
  * Das Regime darf nur kurz sein (schneller EMA unter langsamer EMA).
  * Williams %R (Länge = `WilliamsPeriod`) muss den `WilliamsSellLevel` der letzten Kerze durchqueren, während der vorherige Wert noch unter Null lag.
  * CCI (Länge = `CciPeriod`) muss größer oder gleich `CciSellLevel` sein.
  * Die Nettoposition muss flach sein; Die Strategie eröffnet höchstens `MaxOpenPositions` Trades und verwendet standardmäßig eine einzelne Marktorder der Größe `EntryVolume`.
* **Long-Trades**
  * Das Regime muss nur lang sein (schneller EMA über langsamer EMA).
  * Williams %R muss den Wert `WilliamsBuyLevel` überschreiten, während der vorherige Wert noch unter Null lag.
  * CCI muss kleiner oder gleich `CciBuyLevel` sein.
  * Die Nettoposition muss flach sein, bevor eine neue Long-Position eingeleitet wird.

### Ausgangsregeln
* **Hard Stops** – `StopLossPips` und `TakeProfitPips` werden mithilfe der Preisstufe des Instruments in Preisversätze umgewandelt. Ein Wert von `0` deaktiviert die entsprechende Schutzstufe.
* **Donchian Ausbruch** – wenn der Preis über dem vorherigen Donchian oberen Band (Länge = `DonchianLength`) schließt, wird jede Short-Position sofort geschlossen. Ein Schlusskurs unterhalb des vorherigen unteren Bandes schließt Long-Positionen. Der Kanal verwendet den zuvor abgeschlossenen Wert, um die Verzögerung von `iHighest`/`iLowest` gegenüber EA zu reproduzieren.
* **Regime-Flip** – Immer wenn sich die EMA-Beziehung umkehrt, liquidiert die Strategie das gegnerische Engagement, bevor neue Trades in die neue Richtung zugelassen werden.

### Geldmanagement
* Beginnt mit `EntryVolume` Einheiten pro Trade.
* Wenn bei einer flachen Strategie ein realisierter Gewinn von mehr als `BigWinThreshold` auftritt, wird das Volumen um `VolumeIncrement` erhöht und der Schwellenwert verdoppelt (4 → 8 → 16 usw.). Wenn einer der Parameter auf `0` gesetzt ist, ist die Skalierungsregel deaktiviert.

## Parameter
* **Fast EMA Period** – Länge des schnellen Trendfilters (Standard: 33).
* **Langsamer EMA-Zeitraum** – Länge des langsamen Trendfilters (Standard: 70).
* **Williams %R-Periode** – Lookback für den Williams %R-Oszillator (Standard: 62).
* **Williams Verkaufsniveau** – Obergrenze, die für Short-Signale nach unten überschritten werden muss (Standard: −20).
* **Williams Kaufniveau** – untere Grenze, die für Long-Signale nach oben überschritten werden muss (Standard: −80).
* **CCI Zeitraum** – Lookback für den Commodity Channel Index (Standard: 26).
* **CCI-Verkaufsniveau** – Mindestwert von CCI, der erforderlich ist, um Leerverkäufe zuzulassen (Standard: 101).
* **CCI Kauflevel** – maximaler CCI-Wert, der erforderlich ist, um Long-Positionen zuzulassen (Standard: 99).
* **Donchian Länge** – Anzahl der abgeschlossenen Kerzen, die für den Ausbruchsausgang abgetastet wurden (Standard: 410).
* **Max. offene Positionen** – maximale Anzahl gleichzeitiger Trades; Das klassische Setup verwendet einen Vertrag (Standard: 1).
* **Take Profit (Pips)** – Gewinnziel gemessen in Preisschritten (Standard: 360).
* **Stop-Loss (Pips)** – Schutzstopp, gemessen in Preisschritten (Standard: 20).
* **Einstiegsvolumen** – Startordergröße (Standard: 0,01).
* **Grenzwert für große Gewinne** – realisierter Gewinn, der vor einer Vergrößerung erforderlich ist (Standard: 4,0).
* **Volumenerhöhung** – zusätzliches Volumen hinzugefügt, nachdem der Schwellenwert überschritten wurde (Standard: 0,01).
* **Kerzentyp** – Zeitrahmen, der für alle Indikatorberechnungen verwendet wird (Standard: stündliche Kerzen).

## Zusätzliche Hinweise
* Die Pip-Konvertierung basiert auf dem `PriceStep` des Wertpapiers. Instrumente ohne Preisschritt fallen auf einen einheitlichen Pip-Wert zurück.
* Donchian-Level werden absichtlich um eine Kerze verzögert, sodass der Exit die `shift=1`-Logik der ursprünglichen MetaTrader-Aufrufe widerspiegelt.
* Die Volumenskalierung wertet nur den realisierten PnL aus, während die Position flach ist, und verhindert so, dass schwankende Gewinne Fehlalarme auslösen.
* Die in der Quelle EA vorhandenen UI-Beschriftungsobjekte werden weggelassen, da StockSharp den Status durch Diagramme und Protokolle visualisiert.
* In diesem Paket wird nur die C#-Implementierung bereitgestellt; Es gibt keine Python-Version.
