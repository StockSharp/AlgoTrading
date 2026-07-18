# BreakOut15-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
BreakOut15 ist eine 15-minütige Breakout-Strategie, die vom MetaTrader 4-Expertenberater „BreakOut15.mq4“ übernommen wurde. Die Strategie kombiniert einen Crossover-Filter mit gleitendem Durchschnitt mit Breakout-Ausführung und mehrstufigem Trailing-Schutz. Bestellungen werden über die übergeordnete StockSharp API gesendet und basieren nur auf fertigen Kerzen.

## Kernlogik
1. Berechnen Sie zwei konfigurierbare gleitende Durchschnitte (schnell und langsam) mit der ausgewählten Methode, dem Zeitraum, der Verschiebung und dem angewendeten Preis.
2. Wenn der schnelle Durchschnitt den langsamen Durchschnitt überschreitet, planen Sie einen langen Ausbruchspreis bei `Close + BreakoutLevel * PriceStep`. Ein rückläufiger Crossover plant einen kurzen Ausbruch bei `Close - BreakoutLevel * PriceStep`.
3. Ausstehende Breakout-Preise werden gelöscht, wenn die Crossover-Bedingung verschwindet, die Handelszeiten enden oder ein Breakout in die entgegengesetzte Richtung aktiv wird.
4. Markteintritte werden ausgeführt, sobald die Kerze das ausstehende Niveau durchbricht und die Aktien- und Risikoprüfungen erfolgreich sind.
5. Offene Positionen werden durch Stop-Loss, Take-Profit und einen von drei Trailing-Stop-Modi verwaltet. Crossbacks mit gleitendem Durchschnitt erzwingen einen sofortigen Ausstieg.
6. Optionale Zeitfilter verhindern neue Trades außerhalb des konfigurierten Fensters und können Positionen freitags spät auflösen.

## Money-Management
* **UseMoneyManagement / TradeSizePercent** – ermöglicht risikobasierte Größenbestimmung. Die Positionsgröße entspricht dem ganzzahligen Teil von `floor(equity * percent / 10000) / 10`, mit einem Minimum von 1 Lot.
* **FixedVolume** – Fallback-Größe, wenn die Geldverwaltung deaktiviert ist oder kein Eigenkapital verfügbar ist.
* **MaxVolume** – begrenzt jedes berechnete Volumen.
* **MinimumEquity** – blockiert neue Trades, wenn das Eigenkapital unter den Schwellenwert fällt.

## Risikomanagement
* **StopLossPips / TakeProfitPips** – klassische Schutz-Offsets, gemessen in Pips (umgerechnet über die Preisstufe des Instruments).
* **UseTrailingStop** – aktiviert die dynamische Stop-Verarbeitung, sobald eine Position vorhanden ist.
* **TrailingStopType**
  * `Immediate`: Sofort um die ursprüngliche Stop-Loss-Distanz nachlaufen.
  * `Delayed`: Warten Sie auf einen Gewinn von `TrailingStopPips`, bevor Sie auf diese Distanz zurückfallen.
  * `MultiLevel`: Gewinne bei drei programmierbaren Meilensteinen (`Level1/2/3TriggerPips`) sichern und dann um `Level3TrailingPips` nachlaufen.

## Handelsplan
* **UseTimeLimit, StartHour, StopHour** – Handel nur innerhalb des angegebenen Stundenintervalls zulassen.
* **UseFridayClose, FridayCloseHour** – optional alle Positionen spät am Freitag reduzieren.

## Indikatoren und Daten
* **Schnelle/langsame gleitende Durchschnitte** – Wählen Sie zwischen der einfachen, exponentiellen, geglätteten, linear gewichteten oder der Methode der kleinsten Quadrate.
* **Angewandte Preismodi** – reproduzieren MT4-Preisquellen (Schluss, Eröffnung, Hoch, Tief, Median, typisch, gewichtet).
* **CandleType** – standardmäßig sind Kerzen mit einem Zeitrahmen von 15 Minuten eingestellt, können aber bei Bedarf geändert werden.

## Zusätzliche Hinweise
* Die Strategie synchronisiert automatisch Einstiegs-, Stopp- und Zielpreise mit dem aktuellen durchschnittlichen Positionspreis, sodass nachlaufende Anpassungen die tatsächlichen Ausführungen widerspiegeln.
* Alle Berechnungen hängen vom Instrument `PriceStep` ab; Stellen Sie sicher, dass es mit dem gehandelten Markt übereinstimmt.
* Tests sollten die Auslösung von Ausbrüchen, Trailing-Stop-Übergänge und Rundungsregeln für das Geldmanagement in bullischen und bärischen Szenarien validieren.
