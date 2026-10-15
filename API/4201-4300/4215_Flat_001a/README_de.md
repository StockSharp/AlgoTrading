# Flat 001a Range-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Flat 001a ist ein Scalping-System, das für den EURUSD-Stundenchart entwickelt wurde. Es scannt die letzten drei Stundenkerzen und misst den Abstand zwischen dem höchsten Hoch und dem niedrigsten Tief. Wenn die Spanne dieses Drei-Kerzen-Fensters innerhalb einer konfigurierbaren Anzahl von Punkten bleibt, geht die Strategie davon aus, dass der Preis innerhalb der Flat gefangen bleibt. Anschließend wird versucht, kurzfristige Ausschläge in das obere oder untere Viertel des Kanals abzuschwächen, und es werden sofort Schutzanordnungen erlassen.

Der ursprüngliche MQL4-Expertenberater handelte im ersten Halbjahr nur mit EURUSD und lehnte den Handel ab, wenn das Symbol oder der Zeitrahmen falsch war. Dieser Port behält die gleichen Standardwerte bei (EURUSD, 60-Minuten-Kerzen) und reproduziert alle Einstiegs-, Stop-Loss-, Take-Profit- und Trailing-Stop-Berechnungen in StockSharp.

## Indikatoren und Daten
- Die Indikatoren `Highest` und `Lowest` (Periode = 3) verfolgen die Ober- und Unterseite der letzten drei abgeschlossenen Kerzen.
- Ein Zeitrahmenparameter ist standardmäßig auf 60-Minuten-Kerzen eingestellt, um die H1-Anforderung des Quellcodes widerzuspiegeln.
- Da keine zusätzlichen Oszillatoren oder Glättungsfilter verwendet werden, reagiert die Strategie ausschließlich auf rohe Preisextreme.

## Eingabelogik
1. Warten Sie, bis die Abonnementkerze geschlossen ist. Es werden nur fertige Kerzen verarbeitet.
2. Stellen Sie sicher, dass der aktuelle Sicherheitscode mit dem konfigurierten Code übereinstimmt (Standard: `EURUSD`). Ist dies nicht der Fall, bleibt die Strategie im Leerlauf.
3. Bewerten Sie das optionale Handelsfenster. Standardmäßig sind Zutritte während der zwei Stunden ab Mitternacht der Bahnsteigzeit (Stunden 0 und 1) erlaubt. Der Filter kann deaktiviert werden.
4. Berechnen Sie den Drei-Kerzen-Bereich `range = highest - lowest` und übersetzen Sie ihn mit dem Instrument `PriceStep` in Punkte.
5. Fahren Sie nur fort, wenn die Anzahl der Punkte zwischen `DiffMinPoints` und `DiffMaxPoints` liegt.
6. Wenn der Schlusskurs im niedrigsten Viertel der Spanne liegt und keine Position offen ist, gehen Sie einen Long-Trade ein.
7. Wenn der Schlusskurs im höchsten Viertel der Spanne liegt und keine Position offen ist, gehen Sie einen Short-Trade ein.

## Auftragsverwaltung
- **Anfänglicher Stop-Loss**
  - Lange Trades: `lowest - range / 3`.
  - Short-Trades: `highest + range / 3`.
- **Take-Profit**
  - Long-Trades: Einstiegspreis + `TakeProfitPoints * PriceStep`.
  - Short-Trades: Einstiegspreis − `TakeProfitPoints * PriceStep`.
- **Trailing-Stop**
  - Sobald der nicht realisierte Gewinn `TrailingStopPoints * PriceStep` übersteigt, wird der Stop-Loss Kerze für Kerze nachgezogen.
  - Long-Trades verschieben den Stop auf `closePrice - TrailingDistance`, wenn dieser höher als der aktuelle Stop ist.
  - Short-Trades verschieben den Stop auf `closePrice + TrailingDistance`, wenn dieser niedriger als der aktuelle Stop ist.
- Alle Exits werden mit Marktaufträgen ausgeführt. Die Strategie schließt die gesamte Position, wenn entweder das Stop-Loss- oder Take-Profit-Niveau von der nachfolgenden Kerze berührt wird.

## Parameter
| Gruppe | Name | Beschreibung | Standard |
| --- | --- | --- | --- |
| Allgemein | `CandleType` | Für Berechnungen verwendeter Kerzentyp. Sollte auf einen Zeitrahmen von 60 Minuten eingestellt werden, um dem ursprünglichen System zu entsprechen. | `TimeFrame(60m)` |
| Allgemein | `SecurityCode` | Erwarteter Sicherheitscode. Lassen Sie das Feld leer, um mit einem beliebigen Instrument zu handeln. | `EURUSD` |
| Bereichsfilter | `DiffMinPoints` | Mindestspanne von drei Kerzen in Punkten, die für den Handel erforderlich sind. | `18` |
| Bereichsfilter | `DiffMaxPoints` | Maximal zulässiger Drei-Kerzen-Bereich in Punkten für den Handel. | `28` |
| Handelsfenster | `EnableTimeFilter` | Aktiviert oder deaktiviert den Stundenfilter. | `true` |
| Handelsfenster | `OpenHour` | Startstunde (0–23) für das Handelsfenster. Die Strategie ermöglicht auch die unmittelbare nächste Stunde. | `0` |
| Risikomanagement | `TakeProfitPoints` | Take-Profit-Distanz ausgedrückt in Punkten. Zum Deaktivieren auf Null setzen. | `8` |
| Risikomanagement | `TrailingStopPoints` | Trailing-Stop-Distanz, ausgedrückt in Punkten. Auf Null setzen, um das Nachziehen zu deaktivieren. | `6` |

## Praktische Hinweise
- Die Eigenschaft StockSharp `Strategy.Volume` steuert die Bestellgröße. Passen Sie es an die Größe Ihres Maklervertrags an.
- Stellen Sie sicher, dass das ausgewählte Instrument einen gültigen `PriceStep` bereitstellt. Wenn `PriceStep` fehlt, greift die Strategie auf `1` zurück und protokolliert eine Warnung.
- Der MQL4-Expertenberater bot eine optionale Geldverwaltung durch Skalierung der Lose entsprechend dem Kontostand an. Das Beispiel von StockSharp hält die Positionsgröße konstant; Bei Bedarf können Sie Ihr eigenes Volume-Management per Skript erstellen.
- Testen Sie die Strategie immer in einer Simulation, bevor Sie sie live ausführen. Die abschließende Logik geht davon aus, dass der Broker Schutzaufträge ausführt, wenn die Extremwerte der Kerze das Niveau überschreiten. In schnellen Märkten kann Slippage das realisierte Risiko erhöhen.
