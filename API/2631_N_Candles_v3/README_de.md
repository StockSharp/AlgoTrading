# N Candles v3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie scannt die zuletzt abgeschlossenen Kerzen und sucht nach einer Sequenz, bei der die letzten *N* Bars dieselbe Richtung teilen (alle bullisch oder alle bärisch). Wenn eine solche Strähne erscheint, geht sie in Richtung der Sequenz vor, wobei eine Obergrenze für die Anzahl gleichzeitig eröffneter Positionen eingehalten wird. Die Implementierung migriert den ursprünglichen MetaTrader 5 Expert Advisor auf die StockSharp High-Level-API.

## Handelslogik
- Die Engine abonniert den konfigurierten Kerzentyp und verarbeitet nur abgeschlossene Bars.
- Für jede abgeschlossene Kerze wird die Körperrichtung bewertet: bullisch, bärisch oder neutral (Doji).
- Doji-Kerzen setzen den internen Zähler zurück. Andernfalls erhöht sich der Zähler, wenn die aktuelle Kerze dieselbe Richtung wie die vorherigen hat. Sobald der Zähler den Parameter `Identical Candles` erreicht, gibt die Strategie eine neue Order aus.
- Long-Signale schließen zunächst jedes bestehende Short-Exposure und fügen dann eine Long-Einheit hinzu, solange das gesamte gekaufte Volumen unter `Max Positions * Volume` bleibt.
- Short-Signale funktionieren symmetrisch für bärische Strähnen.

## Risikomanagement
- Nach jedem ausgeführten Trade platziert die Strategie neue Schutz-Stop-Loss- und Take-Profit-Orders basierend auf dem durchschnittlichen Einstiegspreis der aktiven Position.
- Abstände werden in Preisschritten des Instruments gemessen: `Take Profit Points` multipliziert den Schritt zur Berechnung des Ziels über (Long) oder unter (Short) dem Einstieg; `Stop Loss Points` verwendet dieselbe Idee für den Schutz-Stop.
- Ein gestufter Trailing Stop kann den anfänglichen Stop ersetzen, sobald sich der Preis um `Trailing Stop Points` zugunsten der Position bewegt. Der Stop wird nur verschoben, wenn der Preis mindestens `Trailing Step Points` über das vorherige Trailing-Niveau hinaus vorangeschritten ist.

## Parameter
- **Candle Type** – Zeitrahmen oder Kerzenquelle zur Analyse.
- **Identical Candles** – Erforderliche Anzahl aufeinanderfolgender Kerzen mit derselben Richtung, um einen Einstieg auszulösen.
- **Volume** – Ordergröße für jeden neuen Einstieg in Instrumenteneinheiten.
- **Max Positions** – Maximale Anzahl an Einstiegseinheiten, die gleichzeitig in dieselbe Richtung offen sein können.
- **Take Profit Points** – Take-Profit-Abstand in Vielfachen des Instrument-Preisschritts.
- **Stop Loss Points** – Stop-Loss-Abstand in Vielfachen des Instrument-Preisschritts.
- **Trailing Stop Points** – Abstand vom aktuellen Preis zur Aktivierung und Aufrechterhaltung des Trailing Stops. Auf null setzen, um Trailing zu deaktivieren.
- **Trailing Step Points** – Zusätzliche Distanz in Preisschritten, die zurückgelegt werden muss, bevor der Trailing Stop erneut verschoben wird.

## Zusätzliche Hinweise
- Die Strategie arbeitet im Netting-Modus: Wenn ein Signal in der entgegengesetzten Richtung erscheint, wird jede bestehende Exposure auf der anderen Seite geschlossen, bevor eine neue Position hinzugefügt wird.
- Alle Schutzorders werden nach jeder Füllung neu erstellt, um ihr Volumen mit der offenen Positionsgröße synchronisiert zu halten.
- Sicherstellen, dass das Instrument einen nicht-null `PriceStep` bereitstellt; andernfalls wird der Standard-Schrittwert von 1 verwendet.
