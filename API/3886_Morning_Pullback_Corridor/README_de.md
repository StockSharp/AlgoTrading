# Strategie für den Pullback-Korridor am Morgen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Morning Pullback Corridor Strategy** repliziert das Verhalten des Expertenberaters „3_Otkat_Sys_v1_2“ MetaTrader 4. Das System handelt einmal pro Tag während der Sitzung am frühen Morgen und bewertet dabei die Interaktion zwischen dem aktuellen Preis und dem Preiskorridor, der durch Kerzen gebildet wird, die 29 Balken voneinander entfernt sind. Es reagiert auf morgendliche Rückschläge nach einer starken Bewegung über Nacht und fügt sofort asymmetrische Take-Profit-Niveaus für Long- und Short-Positionen hinzu.

## Handelslogik
1. **Sitzungsfilter** – Aufträge werden nur innerhalb der konfigurierten Handelsstunde (Standard 05:00 Uhr Plattformzeit) und während der ersten paar Minuten dieser Stunde berücksichtigt. Montags und freitags sind gemäß der ursprünglichen EA ausgenommen.
2. **Preiskorridorberechnungen** – für jede abgeschlossene Kerze behält die Strategie ein fortlaufendes Fenster der neuesten Balken bei. Es vergleicht:
   - der Eröffnungspreis 29 Balken zurück zum vorherigen Kerzenschluss (`Open[29] - Close[1]`),
   - die vorherige Kerze schließt mit dem Eröffnungspreis 29 Balken zurück (`Close[1] - Open[29]`),
   - der Abstand vom vorherigen Nahbereich zum tiefsten Tief innerhalb des 29-Bar-Bereichs,
   - der Abstand vom höchsten Hoch im gleichen Bereich zum vorherigen Schlusskurs.
3. **Eintrittsregeln** – wenn die Nachtbewegung den Schwellenwert `CorridorOpenClosePoints` überschreitet und der letzte Pullback in den konfigurierten `PullbackPoints ± CorridorPullbackPoints`-Umschlag passt, wird zu Beginn der Morgensitzung eine Marktposition eröffnet:
   - Long-Einstiege erfordern entweder eine starke Abwärtsbewegung mit einem flachen Rückzug oder eine Aufwärtsbewegung mit einer ausgedehnten Fortsetzung oberhalb des Korridors.
   - Short-Einstiege spiegeln die Logik bärischer Setups wider.
4. **Positionsmanagement** – jeder Trade erhält:
   - ein Stop-Loss bei `StopLossPoints * PriceStep` vom Einstiegspreis,
   - ein Take-Profit bei `TakeProfitPoints * PriceStep` für Short-Positionen und bei `(TakeProfitPoints + LongTakeProfitExtraPoints) * PriceStep` für Long-Positionen.
5. **Täglicher Ausstieg** – jede Position, die nach dem konfigurierten Schließschwellenwert (Standard nach 22:45 Uhr) noch offen ist, wird zwangsweise geschlossen, um ein Halten über Nacht zu vermeiden.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TakeProfitPoints` | Basis-Take-Profit-Distanz in Instrumentenpunkten, angewendet auf Short-Trades. Long-Trades fügen `LongTakeProfitExtraPoints` hinzu. |
| `StopLossPoints` | Schutzanschlagabstand in Instrumentenpunkten. |
| `PullbackPoints` | Gewünschte Pullback-Größe, um die herum die Strategie Retracements bewertet. |
| `CorridorOpenClosePoints` | Mindestabstand zwischen den Preisen, getrennt durch 29 Balken, um einen Übernachtimpuls zu bestätigen. |
| `CorridorPullbackPoints` | Auf den Rückzugsschwellenwert angewendete Toleranz, um den Eingangskorridor zu erstellen. |
| `LongTakeProfitExtraPoints` | Zusätzliche Punkte zum Long-Take-Profit-Ziel hinzugefügt. |
| `TradeHour` | Stunde (0–23), in der neue Einträge zulässig sind. |
| `TradeMinuteLimit` | Maximale Minute innerhalb der Handelsstunde, um neue Signale zu akzeptieren. |
| `CloseHour` | Stunde, in der die Strategie mit der Suche nach zeitbasierten Exits beginnt. |
| `CloseMinuteThreshold` | Minute innerhalb von `CloseHour`, nach der jede offene Position geschlossen wird. |
| `CandleType` | Für Kerzenabonnements verwendeter Zeitrahmen (Standard 1 Minute). |

## Implementierungshinweise
- Die Strategie basiert auf `Security.PriceStep`, um punktbasierte Eingaben in absolute Preisabstände umzuwandeln. Wenn das Instrument keinen gültigen Preisschritt bereitstellt, greift die Logik auf `1.0` zurück.
- Stop-Loss- und Take-Profit-Level werden bei jeder abgeschlossenen Kerze überwacht; Die Strategie schließt Positionen mit Marktaufträgen, sobald das Niveau innerhalb dieses Kerzenbereichs durchbrochen wird.
- Das rollierende Fenster enthält die letzten 60 Kerzen, um die erforderlichen 29-Balken-Berechnungen abzudecken und die in MetaTrader verwendeten `Lowest/Highest`-Helfer nachzuahmen.
- Die Diagrammvisualisierung (Kerzen und eigene Trades) ist automatisch verfügbar, wenn ein Diagrammbereich in der Host-Anwendung erstellt wird.

## Nutzungstipps
- Stellen Sie sicher, dass das Handelskontovolumen (Eigenschaft `Volume`) festgelegt ist, bevor Sie mit der Strategie beginnen. Der EA skaliert die Positionsgröße niemals dynamisch.
- Halten Sie den Datenfeed an der vom ursprünglichen Expertenberater erwarteten Sitzungszeitzone ausgerichtet, um ein identisches Verhalten aufrechtzuerhalten.
- Optimieren Sie die Korridorparameter, wenn Sie die Strategie auf Märkte mit unterschiedlichen Volatilitätsprofilen anwenden, da die punktbasierten Schwellenwerte für das Originalinstrument angepasst wurden.
