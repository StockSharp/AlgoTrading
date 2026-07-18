# Skalpell-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Scalpel Strategy** ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `Scalpel.mq4`. Das System sucht nach Momentumausbrüchen im Basiszeitrahmen, bestätigt die Bewegung mit Tiefs/Hochs im höheren Zeitrahmen und filtert die Einträge mithilfe einer gerichteten Volumenstudie, die auf 1-Minuten-Kerzen basiert. Das Positionsmanagement spiegelt das ursprüngliche EA wider: Gewinne werden mit einem festen Take-Profit erzielt, der mit der Zeit schrumpft, der Stop-Loss kann nachgeben, sobald sich der Preis zugunsten des Handels bewegt hat, und jede Position kann nach einer konfigurierbaren Lebensdauer oder am Freitagabend zwangsweise geschlossen werden.

## Handelslogik
- **Multi-Timeframe-Trendfilter**: Ein Long-Signal erfordert, dass die aktuellen Tiefststände der H4-, H1- und M30-Kerzen höher sind als ihre vorherigen Tiefststände. Kurze Signale erfordern im gleichen Zeitrahmen niedrigere Höchstwerte.
- **Breakout-Bestätigung**: Die Strategie wartet darauf, dass der beste Brief das vorherige Hoch (Long) überschreitet oder das beste Geld unter das vorherige Tief (Short) im Basiszeitrahmen fällt. Zusätzlich müssen die vorherigen drei Hochs (oder Tiefs) eine Treppe in Ausbruchsrichtung bilden.
- **CCI Fenster**: Der Commodity Channel Index der vorherigen geschlossenen Kerze muss innerhalb eines konfigurierbaren Bandes um Null bleiben. Positive Grenzwerte verwenden ein symmetrisches Fenster; Negative Grenzwerte lockern die Anforderungen für eine der Seiten genau wie im Original EA.
- **Directional Volume Filter**: Volumina aus dem Volatilitätszeitraum werden in zwei rollierende Blöcke aufgeteilt. Ein Handel ist nur zulässig, wenn der jüngste Block mehr Richtungsvolumen aufweist als der ältere Block und der ältere Block ungleich Null ist. Negative `VolatilityWindow`-Werte schalten den Filter auf bereichsbasierte (ungerichtete) Akkumulation um.
- **Risikomanagement**:
  - Feste Take-Profit- und Stop-Loss-Abstände, ausgedrückt in Mindestpreisschritten.
  - Das Take-Profit-Niveau wird alle `TakeProfitReduceMinutes` Minuten, in denen die Position offen bleibt, um einen Preisschritt reduziert.
  - Ein Trailing Stop wird aktiviert, nachdem sich der Preis um `TrailingStopPoints` bewegt hat, und folgt dann der Bewegung Kerze für Kerze.
  - Positionen können nach `LiveMinutes` oder zum konfigurierten `FridayCloseHour` zwangsweise geschlossen werden.
  - Neue Einträge werden blockiert, solange die absolute Nettoposition gleich `MaxDirectionalPositions * TradeVolume` ist und optional, solange die Abklingzeit für den erneuten Eintritt aktiv ist.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `TradeVolume` | `-5` | Bestellgröße. Positive Werte verwenden feste Lots; Negative Werte stellen einen Prozentsatz des Portfoliokapitals dar, der unter Verwendung des aktuellen Briefkurses in Volumen umgewandelt wird. |
| `TakeProfitPoints` | `40` | Abstand vom Einstieg bis zum Take-Profit-Ziel in Preisschritten. |
| `StopLossPoints` | `340` | Abstand vom Einstieg bis zum Stop-Loss in Preisschritten. |
| `TrailingStopPoints` | `25` | Trailing-Stop-Distanz in Preisschritten. Der Trail wird aktiviert, sobald die Bewegung diese Distanz überschreitet. |
| `CciPeriod` | `14` | Lookback-Zeitraum für den Commodity Channel Index, berechnet auf Basis des Basiszeitraums. |
| `CciLimit` | `75` | Obergrenze für lange Einträge und gespiegelte negative Grenze für kurze Einträge. Negative Werte reproduzieren die asymmetrischen Grenzen des ursprünglichen EA. |
| `MaxDirectionalPositions` | `1` | Maximal zulässige Nettopositionseinheiten (in Vielfachen des berechneten Handelsvolumens) in eine Richtung. |
| `ReentryIntervalMinutes` | `0` | Mindestwartezeit in Minuten zwischen zwei aufeinanderfolgenden Einträgen. |
| `TakeProfitReduceMinutes` | `600` | Minuten bevor die Take-Profit-Schwelle um eine Preisstufe reduziert wird. Auf `0` setzen, um die Reduzierung zu deaktivieren. |
| `LiveMinutes` | `0` | Maximale Lebensdauer einer Position in Minuten. Ein Wert von `0` deaktiviert den Timer. |
| `VolatilityWindow` | `100` | Anzahl der in jedem Rolling Block gespeicherten Volatilitätskerzen. Negative Werte wechseln zur bereichsbasierten Akkumulation, `0` verwendet nur die letzte Kerze. |
| `VolatilityThresholdPoints` | `1` | Mindestkerzenkörper (positives Fenster) oder -bereich (ungerichtetes Fenster), der zur Akkumulation des Volumens erforderlich ist. Das Vorzeichen vertauscht die Interpretation der Lautstärken nach oben/unten. |
| `FridayCloseHour` | `22` | Tageszeit (0-23), die zur Liquidation von Positionen am Freitagabend verwendet wird. `0` deaktiviert den Freitagsausgang. |
| `SpreadLimitPoints` | `5.5` | Maximal zulässiger Spread in Preisschritten beim Eröffnen einer neuen Position. |
| `CandleType` | `1 minute` | Basiszeitrahmen, der Einträge generiert und Ausstiege verwaltet. |
| `Hour1CandleType` | `1 hour` | Für die Bestätigung des H1-Trends wird ein höherer Zeitrahmen verwendet. |
| `Hour4CandleType` | `4 hours` | Für die Bestätigung des H4-Trends wird ein höherer Zeitrahmen verwendet. |
| `Minute30CandleType` | `30 minutes` | Für die Bestätigung des M30-Trends wird ein höherer Zeitrahmen verwendet. |
| `VolatilityCandleType` | `1 minute` | Zeitrahmen, der den Richtungsvolumenfilter speist. |

## Implementierungshinweise
- Die Strategie abonniert das Auftragsbuch, um die neuesten besten Geld-/Briefkurse für die Ausbruchserkennung und Spread-Filterung wiederzuverwenden.
- Alle Indikatorbindungen basieren auf dem übergeordneten API von StockSharp: Der Wert CCI wird über `BindEx` abgerufen, während für höhere Zeitrahmen dedizierte Abonnements verwendet werden.
- Trailing Stops und Take-Profit-Reduzierungen werden im Code und nicht über Schutzaufträge ausgeführt, um das ursprüngliche EA-Verhalten nachzuahmen.
- Negative `TradeVolume`-Werte hängen von den aktuellen Angebotspreis- und Sicherheitsvolumenbeschränkungen ab. Unterschreitet die errechnete Größe die Mindestmenge, wird sie automatisch aufgerundet.

## Nutzung
1. Hängen Sie die Strategie an ein Portfolio an und wählen Sie das gewünschte Wertpapier aus.
2. Konfigurieren Sie die Zeitrahmenparameter, Risikoschwellenwerte und Volumengrößenregeln.
3. Starten Sie die Strategie. Signale werden nur bei fertigen Kerzen ausgewertet; Positionen werden mit Marktaufträgen eröffnet und über die integrierten Risikomanagementregeln geschlossen.
