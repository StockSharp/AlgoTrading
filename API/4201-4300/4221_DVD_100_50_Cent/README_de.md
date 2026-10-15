# DVD 100-50-Cent-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die DVD-100-50-Cent-Strategie ist ein konträres Limit-Order-System, das vom ursprünglichen MT4-Expertenberater portiert wurde. Die Logik bewertet den Markt über vier Zeitrahmen (M1, M30, H1, D1) und bewertet potenzielle Setups, bevor Kauf- oder Verkaufslimitaufträge um das nächstgelegene Preisraster der „100-Ebene“ geparkt werden. Wenn die Limit-Order ausgeführt wird, verwaltet die Strategie die Position mit vorberechneten Stop-Loss- und Take-Profit-Levels.

## Indikatoren und Daten
- **RAVI (Range Action Verification Index)** für H1 und D1, berechnet mit SMA(2) und SMA(24) für den Eröffnungspreis.
- **Kerzenrohdaten** zu M1, M30 und H1 für Musterfilter wie Spike-Ablehnung, Konsolidierungsprüfungen und Momentumtests.
- **Rundung des Preisrasters**, die den aktuellen Preis mithilfe einer Zwei-Dezimal-Rundung und einem konfigurierbaren 0,1-Pip-Offset auf die nächste 100er-Ebene bringt.

## Eingabelogik
1. Berechnen Sie den gerundeten „Level 100“-Preis, indem Sie den letzten M1 auf nahezu zwei Dezimalstellen runden und ihn um `PointFromLevelGoPips` verschieben (Standard 50 → 5 Pips).
2. Initialisieren Sie einen internen Score (BAL) bei 0 und addieren/subtrahieren Sie Punkte gemäß:
   - **Trendfilter:** Fügen Sie 10 Punkte hinzu, wenn der H1-RAVI bei Long-Setups unter Null oder bei Short-Setups über Null liegt.
   - **Stündliche Spitzenbestätigung:** Fügen Sie 7 Punkte hinzu, wenn die beiden vorherigen H1-Hochs/Tiefs das Raster um `RiseFilterPips` überschreiten.
   - **Strukturausrichtung:** Fügen Sie 45 Punkte hinzu, wenn der aktuelle M1-Schluss wieder das Niveau überschreitet und die letzten drei H1-Tiefs/Höchststände über/unter dem Sicherheitspuffer (`PointFromLevelGoPips ± 30 * 0.1 pip`) bleiben.
   - **Volatilitätswächter:** Ziehen Sie 50 Punkte ab, wenn die jüngsten M1-Hochs/Tiefs `HighLevelPips` überschreiten (Standard 600 → 60 Pips) oder wenn schnelle Impulsausbrüche auftreten, während der D1 RAVI ein starkes Richtungsregime bestätigt.
   - **Bestätigung des Ausbruchs:** 50 Punkte abziehen, wenn die letzten 15 H1-Kerzen nie den Schwellenwert `LowLevel2Pips` überschritten haben.
   - **Konsolidierungsfilter:** 50 Punkte abziehen, wenn die letzten acht M30-Kerzen alle innerhalb des `LowLevelPips`-Bands bleiben.
3. Geben Sie eine Limit-Order nur dann auf, wenn der Endwert mindestens 50 beträgt und kein anderes Risiko (Position oder ausstehende Order) besteht.

## Auftragserteilung
- **Kauflimit:** 10 Pips unter dem letzten M1-Schluss. Der Stop-Loss liegt `StopLossPips` unter dem Grenzpreis, der Take-Profit liegt `TakeProfitPips` darüber. Wenn der D1 RAVI in den letzten vier Tagen eine steigende Treppe zwischen -1 und +5 zeigt, erhält der Take-Profit eine zusätzliche Verlängerung um 25 Pip.
- **Verkaufslimit:** 7 Pips über dem letzten M1-Schluss mit symmetrischen Stop- und Zielregeln. Wenn der D1 RAVI eine fallende Treppe zwischen -5 und -1 zeigt, wird das Ziel um 25 Pips erweitert.
- Ausstehende Bestellungen verfallen automatisch nach `OrderExpiryMinutes` (Standard: 20 Minuten). Bei Stornierung einer Bestellung werden die gespeicherten Schutzstufen zurückgesetzt.

## Positionsmanagement
- Nach der Erfüllung behält die Strategie die gespeicherten Stop-Loss- und Take-Profit-Werte intern bei und gibt Marktausstiegsaufträge aus, wenn der Preis eines der beiden Niveaus erreicht.
- In der portierten Version wird kein Trailing Stop angewendet; Das ursprüngliche EA hat die nachgestellte Logik standardmäßig deaktiviert.
- Neue Geschäfte werden blockiert, solange eine aktive Position oder eine ausstehende Limit-Order besteht.

## Money-Management
- Wenn `UseMoneyManagement` aktiviert ist, ähnelt die Losgröße der MT4-Implementierung: Sie skaliert um `TradeSizePercent` des aktuellen Eigenkapitals, passt sich an Minikonten an und begrenzt das Ergebnis auf `[0.1, MaxVolume]` (Mini) oder `[1, MaxVolume]` (Standard).
- Durch die Deaktivierung der Geldverwaltung wird ein festes Volumen erzwungen, das durch den Parameter `FixedVolume` gesteuert wird.
- Der Handel wird gestoppt, wenn das Portfolioeigenkapital unter `MarginCutoff` fällt.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `AccountIsMini` | Verwenden Sie die Regeln zur Volumenrundung für Minikonten | `true` |
| `UseMoneyManagement` | Aktivieren Sie die adaptive Losgröße | `true` |
| `TradeSizePercent` | Pro Trade zugewiesener Eigenkapitalanteil | `10` |
| `FixedVolume` | Volumen, das verwendet wird, wenn die Geldverwaltung deaktiviert ist | `0.01` |
| `MaxVolume` | Maximal zulässiges Handelsvolumen | `4` |
| `StopLossPips` | Stop-Loss-Distanz in Pips | `210` |
| `TakeProfitPips` | Take-Profit-Distanz in Pips | `18` |
| `PointFromLevelGoPips` | Basisniveauverschiebung in 0,1 Pips | `50` |
| `RiseFilterPips` | Stündlicher Spike-Bestätigungsabstand (0,1 Pips) | `700` |
| `HighLevelPips` | Einminütiger Spike-Ablehnungsschwellenwert (0,1 Pips) | `600` |
| `LowLevelPips` | 30-Minuten-Konsolidierungsband (0,1 Pips) | `250` |
| `LowLevel2Pips` | Stündliche Breakout-Bestätigungsdistanz (0,1 Pips) | `450` |
| `MarginCutoff` | Eigenkapitaluntergrenze verhindert neue Geschäfte | `300` |
| `OrderExpiryMinutes` | Dauer der ausstehenden Bestellung in Minuten | `20` |

## Nutzungshinweise
- Die Konvertierung basiert auf fertigen Kerzen aus jedem Zeitrahmen; Stellen Sie sicher, dass der historische Datenstrom synchronisierte M1-, M30-, H1- und D1-Kerzen bereitstellt.
- Der Schutzstopp und das Schutzziel werden mit Marktaufträgen ausgeführt, um das MT4-Verhalten der angehängten SL/TP-Werte widerzuspiegeln.
- Da die Logik empfindlich auf die Pip-Größe reagiert, stellen Sie sicher, dass die Eigenschaften `PriceStep` und `Decimals` des Instruments das Kursformat korrekt beschreiben.
