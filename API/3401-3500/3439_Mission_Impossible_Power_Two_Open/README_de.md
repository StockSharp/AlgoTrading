# Mission Impossible Power Two Offene Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine StockSharp-Portierung des MetaTrader-Expertenberaters „Mission Impossible Power Two Open“. Es überwacht die Richtung der zuletzt abgeschlossenen Kerze und eröffnet einen neuen Handelskorb in diese Richtung. Wenn sich der Preis gegen den aktiven Korb bewegt, fügt die Strategie neue Durchschnittseinträge gemäß einem festen Pip-Raster hinzu. Das Volumen jedes neuen Eintrags wächst mit dem gleitenden Verlust des Warenkorbs und ahmt die `power`-basierte Größenregel des ursprünglichen EA nach. Ausstiegsziele werden nach jeder Füllung neu berechnet, sodass der gesamte Korb ein einziges Take-Profit- und Stop-Loss-Level aufweist.

## Handelslogik

1. **Signalerkennung** – Bei jeder abgeschlossenen Kerze vergleicht die Strategie den Schlusskurs der vorherigen Kerze mit ihrem Eröffnungskurs.
   - Wenn die vorherige Kerze über ihrem Eröffnungskurs schloss, ist das Long-Signal aktiv.
   - Wenn der Schlusskurs unter dem Eröffnungskurs liegt, ist das Short-Signal aktiv.
   - Ein Innenbalken (schließen gleich öffnen) erzeugt keinen neuen Korb.
2. **Eröffnung des ersten Handels** – Wenn in der signalisierten Richtung kein Raster aktiv ist, platziert die Strategie eine Marktorder mit der Größe `BaseVolume`.
3. **Durchschnittsraster** – Wenn ein Korb vorhanden ist, misst die Strategie weiterhin den Abstand zwischen dem zuletzt gefüllten Preis und dem aktuellen Schlusskurs.
   - Bei Long-Positionen wird ein neuer Eintrag hinzugefügt, sobald der Preis um mindestens `GridStepPips * PriceStep` unter den letzten Füllstand fällt.
   - Bei Short-Positionen wartet die Strategie, bis der Preis um die gleiche Distanz wie bei der letzten Füllung steigt.
   - Das Raster fügt keine neuen Positionen mehr hinzu, nachdem `MaxTrades` Füllungen in der entsprechenden Richtung erreicht wurden.
4. **Dynamisches Volumen** – Vor dem Senden jeder neuen Bestellung berechnet die Strategie den nicht realisierten Verlust des Warenkorbs, multipliziert ihn mit `Power * 0.0001` und addiert das Ergebnis zu `BaseVolume`. Die endgültige Größe wird auf den Schritt des Austauschvolumens gerundet, zwischen den Sicherheitsgrenzen eingeklemmt und durch `MaxVolume` begrenzt.
5. **Exit-Management** – Nach jeder Füllung berechnet die Strategie die gemeinsamen Ziele für den gesamten Warenkorb neu:
   - Bei einer Einzelposition ist der Take-Profit `TakeProfitFirstPips` vom Einstieg entfernt und der Stop-Loss ist `StopLossPips` in die entgegengesetzte Richtung entfernt.
   - Bei zwei oder mehr Positionen sind beide Ebenen am volumengewichteten Durchschnittspreis des Warenkorbs verankert, wobei `TakeProfitNextPips` für die Zielentfernung und `StopLossPips` für die Absicherung verwendet wird.
   - Wenn der Preis entweder den Take-Profit oder den Stop-Loss berührt, werden alle Positionen in dieser Richtung zum Marktwert geschlossen.
6. **Unabhängige Körbe** – Lange und kurze Raster werden unabhängig voneinander verfolgt. Die Strategie kann beides gleichzeitig halten, wenn abwechselnde Signale eintreffen.

## Parameter

| Name | Typ | Standard | Beschreibung |
| ---- | ---- | ------- | ----------- |
| `BaseVolume` | `decimal` | `0.01` | Ursprüngliche Bestellgröße für einen neuen Warenkorb vor der Skalierung. |
| `MaxVolume` | `decimal` | `2` | Hard Cap für eine einzelne Marktorder nach Rundung. |
| `Power` | `decimal` | `13` | Auf den Floating-Verlust angewendeter Multiplikator bei der Berechnung des additiven Volumens für neue Einträge. |
| `StopLossPips` | `int` | `400` | Abstand in Preisschritten, der für den gemeinsamen Stop-Loss verwendet wird. |
| `TakeProfitFirstPips` | `int` | `15` | Take-Profit-Distanz für den allerersten Eintrag in einem Korb. |
| `TakeProfitNextPips` | `int` | `7` | Take-Profit-Distanz für gemittelte Körbe (zwei oder mehr Einträge). |
| `GridStepPips` | `int` | `21` | Minimale nachteilige Bewegung (in Preisschritten), bevor eine weitere Mittelwertbildungseingabe zulässig ist. |
| `MaxTrades` | `int` | `16` | Maximale Anzahl an Grid-Trades pro Richtung. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Kerzen zur Signalerzeugung und Korbverwaltung. |

## Notizen

- Das Auftragsvolumen richtet sich immer nach dem `VolumeStep` des Instruments und wird durch die `MinVolume` und `MaxVolume` des Wertpapiers begrenzt, sofern diese Limits auf der Handelsplattform verfügbar sind.
- Long- und Short-State-Maschinen sind vollständig getrennt, was es der Strategie ermöglicht, abgesicherte Körbe beizubehalten, wenn sich die Marktrichtung schnell ändert.
- Die Schutzniveaus werden bei jeder Füllung neu berechnet und auf den nächsten `PriceStep` gerundet, was der häufigen Take-Profit-Änderungsroutine entspricht, die in der MetaTrader-Version durchgeführt wird.
- Es werden keine Indikatorpuffer verwendet; Alle Entscheidungen basieren auf rohen Kerzendaten und Portfolioinformationen, genau wie in der Quelle EA.
