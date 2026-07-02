# NTK_07 Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die NTK_07-Strategie ist ein symmetrisches Pending-Order-Raster, das ursprünglich für MetaTrader 4 geschrieben wurde. Sie platziert ein Paar Stop-Orders um den aktuellen Preis und verwaltet eine Positionspyramide im Martingal-Stil mit konfigurierbaren Abständen, Stop-Loss-, Take-Profit- und Trailing-Regeln. Der StockSharp-Port behält das ursprüngliche Verhalten bei und macht jede Einstellung als stark typisierten Strategieparameter verfügbar.

Die Strategie stellt kontinuierlich sicher, dass:

* Ein Kaufstopp und ein Verkaufsstopp werden rund um den Markt geparkt, wenn keine aktiven Aufträge vorliegen.
* Nachdem ein Ausbruch ausgeführt wurde, wird die entgegengesetzte ausstehende Order storniert, um eine Absicherung zu verhindern.
* Weitere Aufträge in die gleiche Richtung können mit dem `Multiplier`-fachen der vorherigen Größe hinzugefügt werden, bis der `LotLimit`-Wert überschritten wird.
* Wenn keine weitere Skalierung zulässig ist, wird die aktive Position durch einen Trailing Stop und optional einen dynamisch erweiterten Take Profit geschützt.
* Schutz-Stopp- und Take-Profit-Orders werden automatisch neu erstellt, wenn sich Volumina oder Zielpreise ändern, sodass die gesamte offene Position immer die gleichen Ausstiegsniveaus aufweist.

## Handelslogik

1. **Sitzungsfilter.** Der Handel wird samstags und sonntags oder wenn die aktuelle Stunde außerhalb von `[StartHour, EndHour]` liegt, übersprungen. Der Stundenbereich entspricht der ursprünglichen MT4-Logik: `EndHour = 24` ermöglicht den Handel während des ganzen Tages.
2. **Kapitalprüfung.** Wenn ein Portfolio angeschlossen ist, muss der aktuelle Kontowert mindestens `MinCapital` betragen, bevor ein Auftrag erstellt wird.
3. **Kanalausbruch (optional).** Wenn `ChannelPeriod` größer als Null ist, werden das höchste Hoch und das niedrigste Tief der letzten `ChannelPeriod` abgeschlossenen Kerzen verfolgt. Abhängig von `UseChannelCenter`:
   * `false` – beide ausstehenden Aufträge werden nur übermittelt, wenn der Briefkurs außerhalb des erkannten Bereichs liegt (Breakout-Handel).
   * `true` – Aufträge werden übermittelt, wenn der Preis wieder die Mitte der Spanne erreicht (Mean-Reversion-Stil).
4. **Erste ausstehende Aufträge.** Wenn keine aktiven Aufträge vorhanden sind, wird ein Kaufstopp `NetStepPips` über dem besten Briefkurs und ein Verkaufsstopp `NetStepPips` unter dem besten Gebot platziert. Das Basisvolumen wird durch das Geldverwaltungsmodul definiert.
5. **Positionsskalierung.** Nachdem eine Order ausgeführt wurde, wird die entgegengesetzte ausstehende Order storniert. Wenn bereits ein anderer Auftrag in die gleiche Richtung aktiv ist, wird der nächste ausstehende Auftrag `NetStepPips` mit `RoundVolume(previousVolume × Multiplier)` entfernt platziert. Wenn das nächste Volumen den berechneten `LotLimit` überschreiten würde, stoppt die Strategie das Hinzufügen zum Raster.
6. **Stop-Loss und Take-Profit.** Jedes Mal, wenn sich die offene Position ändert, erstellt die Strategie einen Schutzstopp und (optional) eine Take-Profit-Order für das aggregierte Long- oder Short-Engagement. Die Abstände werden aus `StopLossPips` und `TakeProfitPips` abgeleitet.
7. **Break-Even-Logik.** Wenn sich `UseBreakEven = true` und der Preis um `BreakEvenOffsetPips` über die letzte ausgeführte Order hinaus bewegen, wird der Stop-Loss auf den volumengewichteten durchschnittlichen Einstiegspreis verschoben (gerundet mit `PriceRoundingFactor`).
8. **Trailing-Verhalten.** Wenn der nächste Skalierungsschritt nicht zulässig ist, verwendet die Strategie den höchsten/niedrigsten Kerzenpreis, um den Stop um `TrailingStopPips` in Richtung Markt zu verschieben. Bei `TrailProfit = true` wird auch die Take-Profit-Distanz verschoben, sodass sie immer `TakeProfitPips` vom letzten Kerzenextrem entfernt bleibt. Wenn `UseMovingAverageFilter = true` und der Preis gegen den gleitenden Durchschnitt gehandelt werden, wird die Trailing-Distanz halbiert, wodurch das ursprüngliche Half-Step-Trailing-Verhalten um einen gleitenden Durchschnitt herum nachgeahmt wird.

## Money-Management

Der Port unterstützt die drei ursprünglichen Geldverwaltungsregeln über den Parameter `ManagementMode`:

| Modus | Beschreibung |
| ---- | ----------- |
| `Fixed` | Verwenden Sie `InitialLot` für jede neue Bestellung und begrenzen Sie die Größe pro Bestellung auf `LotLimit`. |
| `BalanceBased` | Berechnen Sie die Startmenge aus dem Portfoliosaldo neu: `ceil(balance / 1000 × PercentRisk / 100)`. Das Ergebnis wird wiederholt durch `Multiplier` dividiert, um die kleinste Rasterordnung zu projizieren, abgerundet durch `LotRoundingFactor`. Der ursprüngliche `LotLimit` wird zur theoretischen maximalen Losgröße. |
| `Progressive` | Behalten Sie `InitialLot` als Basisvolumen bei, aber projizieren Sie die theoretisch größte Ordnung, indem Sie für jede Gitterebene mit `Multiplier` multiplizieren. |

Alle Bestellungen werden mit `LotRoundingFactor` (Standard 10 => 0,1-Schritte) gerundet, während der Break-Even-Preis mit `PriceRoundingFactor` (Standard 10000 => 0,0001-Schritte) gerundet wird.

## Parameter

| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `NetStepPips` | 23 | Abstand zwischen aufeinanderfolgenden Rasterebenen. |
| `StopLossPips` | 115 | Stop-Loss-Distanz gilt für jede Position. Zum Deaktivieren auf 0 setzen. |
| `TakeProfitPips` | 300 | Take-Profit-Distanz für die aggregierte Position. Zum Deaktivieren auf 0 setzen. |
| `TrailingStopPips` | 75 | Trailing-Stop-Distanz wird aktiviert, sobald eine Skalierung nicht mehr möglich ist. |
| `Multiplier` | 1.7 | Volumenmultiplikator für die nächste Rasterebene. |
| `TrailProfit` | `true` | Wenn diese Option aktiviert ist, wird der Take-Profit entlang des Trailing Stop verschoben. |
| `ManagementMode` | `Progressive` | Ausgewählte Geldverwaltungsregel. |
| `InitialLot` | 1 | Grundauftragsvolumen. |
| `LotLimit` | 7 | Maximal zulässige Losgröße für eine einzelne ausstehende Bestellung. |
| `MaxTrades` | 4 | Maximale Anzahl an Rasterebenen. |
| `PercentRisk` | 10 | Prozentsatz des Guthabens, der bei der saldenbasierten Geldverwaltung verwendet wird. |
| `MinCapital` | 5000 | Vor dem Handel erforderlicher Mindestportfoliowert. |
| `UseBreakEven` | `false` | Aktivieren Sie Break-Even-Stopp-Anpassungen. |
| `BreakEvenOffsetPips` | 5 | Gewinnschwelle (in Pips), die für die Gewinnschwelle erforderlich ist. |
| `UseMovingAverageFilter` | `false` | Aktiviert die Trailing-Logik, die den gleitenden Durchschnitt berücksichtigt. |
| `MovingAverageLength` | 100 | Länge des im Filter verwendeten gleitenden Durchschnitts. |
| `MovingAverageShift` | 0 | Auf den gleitenden Durchschnitt angewendete Verschiebung (Werte von vorherigen Kerzen werden verwendet, wenn > 0). |
| `StartHour` | 0 | Frühestmögliche Handelszeit (0–23). |
| `EndHour` | 24 | Letzte erlaubte Handelszeit (einschließlich). |
| `ChannelPeriod` | 0 | Lookback-Fenster für den Breakout-/Center-Filter. Auf 0 setzen, um den Filter zu deaktivieren. |
| `UseChannelCenter` | `false` | Wechseln Sie zwischen den Stileinträgen „Breakout“ (`false`) und „Mittelpunkt“ (`true`). |
| `LotRoundingFactor` | 10 | Teiler, der beim Runden von Volumina verwendet wird. |
| `PriceRoundingFactor` | 10000 | Teiler, der beim Runden des Break-Even-Preises verwendet wird. |
| `CandleType` | 15-minütiger Zeitrahmen | Funktionierender Kerzentyp zur Bereichserkennung und Nachlaufberechnungen. |

## Implementierungshinweise

* Orderbücher werden abonniert, um vor der Platzierung ausstehender Orders die genauen besten Geld-/Briefwerte zu erhalten. Wenn das Buch nicht verfügbar ist, greift die Strategie auf den Schlusskurs der Kerze zurück.
* Schutzstopps und -ziele werden neu erstellt statt geändert, da die übergeordnete API sicherere Helfer für die Registrierung neuer Befehle bereitstellt, anstatt bestehende zu ändern.
* Gleitende durchschnittliche Verschiebungswerte, die über den verfügbaren Verlauf hinausgehen, fallen auf den neuesten Wert zurück, wodurch Nullverweise vermieden werden und das Verhalten gleichzeitig der MetaTrader-Implementierung nahe kommt.
* Alle Preisberechnungen werden durch `Security.ShrinkPrice` normalisiert, sodass Stop- und Limit-Level immer die Tick-Größe des Instruments berücksichtigen.

## Nutzungstipps

1. Konfigurieren Sie `Strategy.Volume`, um den Multiplikator für die fiktive Handelsgröße zu definieren, wenn Ihr Broker eine Skalierung relativ zur Portfoliogröße erfordert.
2. Passen Sie beim Testen von Symbolen mit exotischen Teilstrichgrößen `LotRoundingFactor` und `PriceRoundingFactor` entsprechend an, damit die Rundungsoperationen aussagekräftig bleiben.
3. Die Standardparameter wurden den ursprünglichen EA für EURUSD-H1-Daten zwischen dem 01.01.2008 und dem 01.11.2008 entnommen. Für andere Vermögenswerte oder Zeitrahmen wird eine erneute Optimierung empfohlen.
4. Da sich im Gitter eine große Richtungsbelastung ansammeln kann, überwachen Sie stets die Werte `LotLimit` und `MaxTrades`, um das Risiko unter Kontrolle zu halten.
