# Early-Open-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Hafen des MetaTrader 4 Expert Advisors `earlyOpenTrend.mq4` mit Sitz in `MQL/9826`.
- Handelt einmal pro Richtung und Tag durch Vergleich des aktuellen Preises mit dem Tageseröffnungspreis nach einer Docht-basierten Bestätigung.
- Imitiert die ursprüngliche Zeitfensterlogik, einschließlich der Sommerzeitverschiebung, die die Brokersitzung um eine oder zwei Stunden verschiebt.
- Verwendet StockSharp auf hoher Ebene API mit Kerzenabonnements, automatischem Positionsschutz und integrierter Sitzungsverwaltung.

## Marktlogik
1. Erstellen Sie eine Intraday-Kerzenserie (Standard 15 Minuten) und rekonstruieren Sie die Eröffnungs-, Höchst- und Tiefstwerte des aktuellen Tages.
2. Bestimmen Sie den aktiven DST-Offset: zwischen `SummerTimeStartDay` und `WinterTimeStartDay` subtrahiert die Strategie zwei Stunden von den konfigurierten Sitzungszeiten; andernfalls wird eine Stunde abgezogen. Dadurch wird die ursprüngliche `ZD`-Variable reproduziert.
3. Bewerten Sie Signale nur, wenn die Kerzenstartzeit innerhalb von `[StartHour, EndHour)` nach der DST-Korrektur liegt und die Strategie flach ist.
4. Lange Einrichtung:
   - Die letzte Kerze schloss über dem täglichen Eröffnungspreis.
   - Der Abstand zwischen der Tageseröffnung und dem Tiefststand des aktuellen Tages überschreitet `RangeFilterPips` (umgerechnet in den absoluten Preis unter Verwendung der Pip-Größe des Instruments).
   - Es wurde kein Long-Trade früher am selben Handelstag eröffnet.
5. Kurzer Aufbau:
   - Die letzte Kerze schloss unter dem täglichen Eröffnungspreis.
   - Der Abstand zwischen dem Höchststand des aktuellen Tages und der Tageseröffnung beträgt mehr als `RangeFilterPips`.
   - Am selben Handelstag wurde kein Short-Trade früher eröffnet.
6. Wenn ein Signal ausgelöst wird, gibt die Strategie eine Marktorder mit einem Volumen von `OrderVolume` aus. Der Handelszeitstempel wird gespeichert, um Ausstiege während der Haltezeit zu unterstützen.

## Sitzungs- und Ausgangsregeln
- `EndHour` verhindert neue Einträge nach der angegebenen Zeit (angepasst um den DST-Offset).
- `ClosingHour` erzwingt die Schließung der Position, sobald die korrigierte Serverstunde den konfigurierten Wert erreicht.
- `HoldingHours` legt eine zusätzliche maximale Haltedauer fest; Bei Überschreitung wird die Position unabhängig von der Sitzungszeit geschlossen.
- Jede Handelsrichtung kann höchstens einmal pro Kalendertag ausgeführt werden. Tägliche Flags werden zurückgesetzt, wenn die Strategie einen neuen Sitzungsstart erkennt.

## Risikomanagement
- `StopLossPips` und `TakeProfitPips` werden mithilfe der aus `Security.PriceStep` abgeleiteten Pip-Größe in absolute Preisversätze umgewandelt (5-stellige Symbole multiplizieren den Schritt automatisch mit 10).
- Wenn einer der Parameter größer als Null ist, aktiviert die Strategie `StartProtection` mit Marktausführung und repliziert dabei die ursprüngliche Post-Entry-Logik `OrderModify`.
- Außerhalb der oben beschriebenen erzwungenen Exits wird keine zusätzliche Nachlauflogik angewendet.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `OrderVolume` | 0,1 | Größe jeder Marktorder. |
| `OrderType` | 0 | Richtungsfilter: `0` = beide, `1` = nur lang, `2` = nur kurz. |
| `RangeFilterPips` | 1 | Mindestdochtabstand zwischen der Tagesöffnung und dem gegenüberliegenden Extrem vor dem Eintritt. |
| `TakeProfitPips` | 100 | Take-Profit-Distanz in Pips (0 deaktiviert). |
| `StopLossPips` | 1000 | Stop-Loss-Distanz in Pips (0 deaktiviert). |
| `StartHour` | 7 | Sitzungsstartstunde vor DST-Subtraktion. |
| `EndHour` | 18 | Sitzungsendestunde vor der DST-Subtraktion. |
| `ClosingHour` | 20 | Stunde, die zum Glätten offener Trades verwendet wird. |
| `HoldingHours` | 0 | Maximale Haltezeit in Stunden (0 deaktiviert). |
| `SummerTimeStartDay` | 87 | Erster Tag des Jahres, an dem der zweistündige DST-Offset aktiviert wird. |
| `WinterTimeStartDay` | 297 | Tag im Jahr, an dem der Offset auf eine Stunde zurückgeht. |
| `CandleType` | 15-minütiger Zeitrahmen | Für Berechnungen verwendete Kerzenreihe. |

## Nutzungshinweise
1. Hängen Sie die Strategie an ein Wertpapier an und stellen Sie sicher, dass der Kerzentyp mit der Granularität des Datenfeeds übereinstimmt, mit dem Sie handeln möchten.
2. Passen Sie die Sitzungszeiten an die Serveruhr des Brokers an. Die DST-Parameter können angepasst werden, wenn die lokale Sommerzeit vom standardmäßigen europäischen Zeitplan abweicht.
3. Konfigurieren Sie Pip-basierte Stopps und Ziele entsprechend der Tick-Größe des Instruments. Die Strategie rechnet Pips automatisch anhand des erkannten Pip-Werts um.
4. Starten Sie die Strategie. Es aktualisiert das Tagesprofil für jede abgeschlossene Kerze, wertet die Eintrittskriterien innerhalb des Sitzungsfensters aus und erzwingt die Beschränkung auf einen einzelnen Handel pro Richtung.

## Unterschiede zum ursprünglichen MQL-Experten
- Verwendet fertige Kerzen anstelle von `Bid`/`Ask`-Prüfungen auf Tick-Ebene, was die Eingaben etwas verzögert, aber die Logik in StockSharp deterministisch hält.
- Schutzanordnungen werden über `StartProtection` statt über manuelle `OrderModify`-Aufrufe umgesetzt.
- Grafische Objekte und Statuskommentare aus dem MetaTrader-Diagramm (Rechtecke, Beschriftungen, Spread-Anzeige) werden weggelassen.
- Durch erzwungene Ausstiege bei Sitzungsende wird die Position sofort geschlossen, anstatt unter Wasser auf ein Break-Even-Ziel zu wechseln.

## Testempfehlungen
- Backtest mit Intraday-Daten, die die gesamte Handelssitzung abdecken, sodass die täglichen Höchst-/Tiefstwerte mit der Live-Umgebung übereinstimmen.
- Validieren Sie die DST-Konfiguration, indem Sie Daten sowohl für Sommer- als auch für Winterperioden simulieren.
- Experimentieren Sie mit verschiedenen Dochtschwellenwerten und Sitzungsstunden, um das Verhalten an das Volatilitätsprofil Ihres Brokers anzupassen.
