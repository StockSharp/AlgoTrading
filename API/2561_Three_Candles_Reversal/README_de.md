# Drei-Kerzen-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein getreuer StockSharp-Port des MQL5-Expertenberaters `Exp_ThreeCandles`. Sie sucht nach einer klassischen Drei-Kerzen-Umkehr:

1. Zwei aufeinanderfolgende Kerzen in eine Richtung.
2. Eine dritte Kerze, die die Richtung wechselt und jenseits der mittleren Kerze schließt.
3. Optionale Volumenbestätigung, es sei denn, die älteste Kerze im Muster ist außergewöhnlich groß.

Wenn eine bullische Konfiguration erscheint, schließt der Algorithmus Short-Exposure und kann eine Long-Position eingehen. Eine bärische Konfiguration tut das Gegenteil. Schützende Stop-Loss- und Take-Profit-Level werden mit dem aktuellen Instrumentenpreisschritt angewendet.

## Mustererkennung

Die Strategie hält ein rollierendes Fenster der neuesten `SignalBar + 3` fertiggestellten Kerzen. Bei jedem neuen Balken prüft sie die Kerze beim `SignalBar`-Versatz (Standard: 1 Balken zurück) und die drei älteren Kerzen:

- **Bullische Umkehr** (potentielles Long):
  - Die zwei älteren Kerzen (`SignalBar + 3` und `SignalBar + 2`) sind bärisch.
  - Die mittlere Kerze schließt über dem Tief der ältesten Kerze.
  - Die letzte Kerze vor dem Signal (`SignalBar + 1`) ist bullisch und schließt über der Eröffnung der mittleren Kerze.
- **Bärische Umkehr** (potentielles Short):
  - Spiegelbild-Logik des bullischen Falls.

Ein Volumenfilter spiegelt den ursprünglichen Indikator. Der Filter wird übersprungen, wenn `MaxBarSize` (in Preisschritten) vom Bereich der ältesten Kerze überschritten wird oder wenn `VolumeFilter` auf `None` gesetzt ist. Andernfalls muss die Umkehr `älteres Volumen < mittleres Volumen` **ODER** `aktuelles Volumen > mittleres Volumen` **ODER** `aktuelles Volumen > ältestes Volumen` erfüllen. Tick- und Real-Volumen werden auf das aggregierte Kerzenvolumen abgebildet, da StockSharp die beiden im High-Level-Kerzenstrom nicht unterscheidet.

## Handelsverwaltung

- Wenn `AllowSellExit` aktiviert ist, deckt ein bullisches Muster sofort jede Short-Position ab, bevor ein Long-Einstieg in Betracht gezogen wird. `AllowBuyExit` verhält sich bei Longs bei bärischen Mustern gleich.
- Neue Positionen werden nur geöffnet, wenn die aktuelle Position flach ist und das entsprechende `Allow*Entry`-Flag wahr ist. Die Ordergröße verwendet die Standard-Volumeneinstellungen der Strategie.
- Stop-Loss- und Take-Profit-Distanzen (`StopLossPips`, `TakeProfitPips`) werden in Preisschritten ausgedrückt und bei jeder fertiggestellten Kerze überwacht.
- Der zuletzt verarbeitete bullische/bärische Signalzeitpunkt wird zwischengespeichert, um doppelte Aktionen zu vermeiden, während eine Kerze weiterhin Ticks auslöst.

## Parameter

| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `CandleType` | 4-Stunden-Zeitrahmen | Von der Strategie verarbeitete Kerzenserie. |
| `SignalBar` | 1 | Wie viele Balken zurück das Signal ausgewertet wird. Muss ≥ 0 sein. |
| `MaxBarSize` | 300 | Wenn der älteste Balkenbereich (konvertiert mit `PriceStep`) diesen Wert überschreitet, wird der Volumenfilter übersprungen. Auf 0 setzen, um immer zu überspringen. |
| `VolumeFilter` | `Tick` | Volumenmodus (`Tick`, `Real` oder `None`). Sowohl `Tick` als auch `Real` verwenden `TotalVolume` aus Kerzen. |
| `AllowBuyEntry` | `true` | Long-Einstiege bei bullischen Mustern aktivieren. |
| `AllowSellEntry` | `true` | Short-Einstiege bei bärischen Mustern aktivieren. |
| `AllowBuyExit` | `true` | Schließen von Long-Positionen bei bärischen Mustern erlauben. |
| `AllowSellExit` | `true` | Schließen von Short-Positionen bei bullischen Mustern erlauben. |
| `StopLossPips` | 1000 | Stop-Loss-Distanz in Preisschritten (0 deaktiviert). |
| `TakeProfitPips` | 2000 | Take-Profit-Distanz in Preisschritten (0 deaktiviert). |

## Konvertierungshinweise

- Geldverwaltungsroutinen aus der ursprünglichen MQL5-Include-Datei wurden durch StockSharp's `BuyMarket`/`SellMarket`-Aufrufe ersetzt. Die Positionsgröße folgt daher dem Standard-Volumen der Engine.
- Das Signaltiming spiegelt den Expertenberater wider, indem die Kerze beim `SignalBar`-Versatz ausgewertet und der vorherige Signalzeitstempel gehalten wird.
- E-Mail-, Push- und Soundbenachrichtigungen vom MQL-Indikator werden absichtlich weggelassen.
- Volumenmodi werden beibehalten, aber beide werden auf das aggregierte Kerzenvolumen abgebildet, da separate Tick- und Real-Volumen in der High-Level-API nicht verfügbar sind.
- Alle Kommentare wurden gemäß den Projektrichtlinien auf Englisch umgeschrieben.

Diese Implementierung bleibt dem ursprünglichen Verhalten treu und hält sich dabei an StockSharp's High-Level-Abonnementmodell.
