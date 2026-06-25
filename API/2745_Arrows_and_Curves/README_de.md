# Arrows-and-Curves-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein C#-Port des MetaTrader-5-Expert-Advisors (EA) „Arrows and Curves". Sie repliziert die indikatorbasierte Logik innerhalb von StockSharp mithilfe der High-Level-API. Das System handelt ein einziges Symbol und reagiert auf die benutzerdefinierten Kanalsignale des Arrows-and-Curves-Indikators. Es kann jeweils nur eine Position aktiv sein; jedes neue Signal öffnet entweder einen neuen Trade oder schließt einen bestehenden.

## Strategielogik
- Kerzen des konfigurierbaren Zeitrahmens werden über `SubscribeCandles` gestreamt. Die Verarbeitungsroutine arbeitet nur mit fertigen Kerzen, um das EA-Verhalten bei neuen Balkeneröffnungen zu spiegeln.
- Der Arrows-and-Curves-Kanal wird innerhalb der Strategie neu aufgebaut: Der Algorithmus scannt das höchste Hoch und das tiefste Tief für das `SSP`-Lookback-Fenster, verschoben um den `Relay`-Offset – genau wie der MT5-Indikator. Aus diesen Werten werden zwei Hüllkurven abgeleitet (`Channel %` für das äußere Band und `Channel Stop %` für das innere Band).
- Die Indikator-Zustandsvariablen (`uptrend` und `uptrend2`) werden in exakt derselben Reihenfolge aktualisiert wie im ursprünglichen MQL-Code. Wenn die vorherige Kerze einen Sell-Pfeil erzeugt, bereitet die Strategie einen Long-Einstieg vor; wenn sie einen Buy-Pfeil erzeugt, bereitet sie einen Short-Einstieg vor. Dies spiegelt das EA-Verhalten wider, bei dem Signale mit Index 1 auf dem nächsten Balken gelesen werden.
- Wenn keine Position offen ist, wird das gespeicherte Signal der vorherigen Kerze verwendet, um eine Marktorder in der entgegengesetzten Richtung des Pfeils zu eröffnen (Sell-Pfeil → Kauftrade, Buy-Pfeil → Verkauftrade).
- Wenn bereits eine Position besteht und ein entgegengesetztes Signal erscheint, wird die aktuelle Position geschlossen, aber eine Umkehrposition wird nicht sofort eröffnet – was der MT5-Quelle entspricht, bei der zuerst geschlossen wird und Einstiege erst auf dem nächsten Balken erneut bewertet werden.

## Risikomanagement
- Stop-Loss- und Take-Profit-Abstände werden in Pips definiert und mithilfe des Instrument-`PriceStep` in absolute Preisoffsets umgerechnet. Bei Instrumenten, die mit 3 oder 5 Dezimalstellen notiert werden, multipliziert die Konvertierung den Schritt mit zehn und reproduziert damit die Pip-Anpassungen des EA.
- Die Trailing-Stop-Funktionalität spiegelt den EA wider: Sobald der schwebende Gewinn `Trailing Stop + Trailing Step` übersteigt, wird der Schutz-Stop um die konfigurierte Distanz nachgezogen, unter Berücksichtigung des Mindestschritts.
- Schutzlevel werden auf jeder abgeschlossenen Kerze geprüft, indem das Hoch/Tief der Kerze zur Annäherung von Intrabar-Auslösern verwendet wird.
- Die Positionsgröße kann über den Parameter `Volume` fixiert werden. Wenn `Volume` auf null gesetzt ist, leitet die Strategie eine dynamische Menge ab, indem `Risk %` des Portfoliowerts gegen die konfigurierte Stop-Loss-Distanz riskiert wird.

## Parameter
- `Volume`: feste Ordergröße. Auf null setzen, um risikobasiertes Sizing zu aktivieren.
- `Risk %`: Prozentsatz des Portfoliowerts, der riskiert wird, wenn das Volumen null ist.
- `Stop Loss (pips)`: Abstand des Schutz-Stops in Pips.
- `Take Profit (pips)`: Abstand des Gewinnziels in Pips.
- `Trailing Stop (pips)`: Trailing-Stop-Abstand in Pips; auf null setzen zum Deaktivieren.
- `Trailing Step (pips)`: minimale zusätzliche Bewegung, bevor der Trailing Stop erneut verschoben wird.
- `SSP`: Anzahl der Kerzen zur Berechnung des Kanalbereichs.
- `Channel %`: äußerer Hüllkurven-Prozentsatz, identisch mit der MT5-Einstellung.
- `Channel Stop %`: innerer Hüllkurven-Prozentsatz, der zum Umschalten des Sekundärzustands verwendet wird.
- `Relay`: Verschiebung, die auf die Kanalberechnung angewendet wird.
- `Candle Type`: Zeitrahmen oder Kerzentyp, der den Indikator speist.

## Implementierungshinweise
- Die Strategie speichert nur die minimale Menge an historischen Hochs, Tiefs und Schlusskursen, die der Indikator benötigt (`SSP + Relay + 5` Balken).
- Alle Kommentare und Hilfsmethoden sind auf Englisch verfasst, um den Repository-Richtlinien zu entsprechen.
- Im Gegensatz zu MT5 werden Stop-Loss- und Take-Profit-Orders auf Kerzendaten simuliert, sodass Intrabar-Ausführungen vom ursprünglichen EA abweichen können. Alles andere folgt denselben Entscheidungsregeln und macht den Port dem Quellskript treu.
