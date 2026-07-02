# Tägliche STP-Eintrittsrahmenstrategie (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Daily STP Entry Frame-Strategie** repliziert das Verhalten des ursprünglichen MetaTrader-Expertenberaters „Daily STP Entry Frame“ unter Verwendung des StockSharp-High-Levels API. Das System bereitet zu Beginn jedes neuen Handelstages Breakout-Stop-Orders vor. Die Einstiegspreise werden aus den Höchst- und Tiefstständen des Vortages abgeleitet, mit zusätzlichen Filtern, um sicherzustellen, dass sich der Markt in der Nähe dieser Extremwerte positioniert, bevor Aufträge erteilt werden. Die Logik ist auf Instrumente im Forex-Stil zugeschnitten, bei denen „Basispunkte“ einem Zehntel Pip für fünfstellige Kurse entsprechen.

## Kern-Workflow
1. **Tägliche Range-Verfolgung** – Die Strategie abonniert tägliche Kerzen, um sich an die Höchst- und Tiefststände der vorherigen Sitzung zu erinnern.
2. **Echtzeitüberwachung** – Level1-Daten liefern die aktuellen Geld-, Brief- und letzten Handelspreise für das Intraday-Management.
3. **Order-Aktivierung** – Wenn zu Beginn eines neuen Tages der letzte Preis mindestens `ThresholdPoints` vom gestrigen Hoch/Tief entfernt liegt und die Eröffnung des aktuellen Tages auf der richtigen Seite dieses Extrems liegt, wird eine Stop-Order übermittelt:
   - Kaufstopp bei `High + SpreadPoints / 2` (umgerechnet in Preiseinheiten).
   - Verkaufsstopp bei `Low - SpreadPoints / 2`.
4. **Risikovalidierung** – Neue Aufträge werden blockiert, wenn der Aktienrückgang `MaximumDrawdownPercent` überschreitet oder die Zeitfilter den Handel nicht zulassen (Wochenenden, Stundenfilter oder Tagesfilter).
5. **Positionsmanagement** – sobald ein Handel aktiv ist, erzwingt die Strategie Folgendes:
   - Statische Stop-Loss- und Take-Profit-Distanzen.
   - Optionaler zeitbasierter Exit nach `CloseAfterSeconds`.
   - Optionaler Trailing-Stop, der den ursprünglichen Parameter „SL-Steigung“ emuliert.
6. **Hygiene am Ende des Tages** – ausstehende Orders werden nach `NoNewOrdersHour` (oder dem entsprechenden Freitagsschluss) und bei jeder Änderung des Kalendertags storniert.

## Handelsregeln
- **Lange Einträge**
  - Zulässig, wenn `SideFilter` `0` (beide) oder `1` (nur lang) ist.
  - Höchststand des Vortages minus aktueller Preis ≥ `ThresholdPoints`.
  - Der heutige Eröffnungskurs liegt unter dem gestrigen Hoch.
  - Der berechnete Einstiegspreis muss den Mindestabstand zum aktuellen Brief einhalten.
- **Kurze Einträge**
  - Zulässig, wenn `SideFilter` `0` (beide) oder `-1` (nur kurz) ist.
  - Aktueller Preis minus Tiefststand des Vortages ≥ `ThresholdPoints`.
  - Der heutige Eröffnungskurs liegt über dem gestrigen Tief.
  - Der berechnete Einstiegspreis muss den Mindestabstand zum aktuellen Gebot einhalten.
- **Geldmanagement**
  - Bei der dynamischen Volumengröße wird ein Prozentsatz des kumulierten Gewinns (`PercentOfProfit`) verwendet.
  - Die resultierende Größe wird durch `MinVolume` und `MaxVolume` begrenzt und an der `VolumeStep` des Instruments ausgerichtet.
  - Der Handel wird automatisch unterbrochen, sobald der gemessene Drawdown `MaximumDrawdownPercent` überschreitet.
- **Schutzlogik**
  - Stop-Loss- und Take-Profit-Level werden in Basispunkten ausgedrückt und anhand der Pip-Größe des Instruments in Preisversätze umgerechnet.
  - Trailing Stop ist nur aktiv, wenn `TrailingSlope < 1`. Es verschiebt die Schutzschwelle näher an den Preis, wenn der nicht realisierte Gewinn wächst.
  - Lebenszeitausgänge schließen alle offenen Positionen, sobald die konfigurierte Anzahl von Sekunden verstrichen ist.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen, der zum Abrufen der Referenzkerzen verwendet wird (standardmäßig täglich). |
| `StopLossPoints` | Stop-Loss-Distanz in Basispunkten. |
| `TakeProfitPoints` | Take-Profit-Distanz in Basispunkten. |
| `TrailingSlope` | Teil des Gewinns, der während des Trailings einbehalten wird; ≥ 1 deaktiviert die Funktion. |
| `SideFilter` | -1 nur kurz, 0 beide Richtungen, 1 nur lang. |
| `ThresholdPoints` | Minimale Lücke zwischen dem aktuellen Preis und dem vorherigen Extrem, die erforderlich ist, um einen Stopp zu aktivieren. |
| `SpreadPoints` | Zusätzlicher Versatz (die Hälfte wird über/unter dem Extremwert verwendet), um die Streuung auszugleichen. |
| `SlippagePoints` | Sicherheitspuffer zur Prüfung des Mindestanhaltewegs hinzugefügt. |
| `NoNewOrdersHour` | Stunde (Plattformzeit), um ausstehende Orders an regulären Tagen zu stornieren. |
| `NoNewOrdersHourFriday` | Freitagsspezifische Stornierungsstunde. |
| `EarliestOrderHour` | Früheste Stunde des Tages, zu der neue Aufträge erstellt werden können. |
| `DayFilter` | 6 für alle Tage oder 0-5, um nur von Sonntag bis Freitag zu handeln. |
| `CloseAfterSeconds` | Optionaler zeitbasierter Exit (0 deaktiviert). |
| `PercentOfProfit` | Bruchteil des kumulierten Gewinns, der zur Skalierung der Positionsgröße verwendet wird. |
| `MinVolume` / `MaxVolume` | Feste Grenzen für das berechnete Volumen. |
| `MaximumDrawdownPercent` | Drawdown-Schwelle, die neue Aufträge blockiert. |

## Konvertierungshinweise
- Die Pip-Konvertierung spiegelt die MetaTrader-Implementierung wider: Wenn die Sicherheit 3 oder 5 Dezimalstellen offenlegt, wird der Basispunkt zu `PriceStep * 10`.
- Das Stopp-Order-Stornierungsfenster reproduziert die abendliche Aufräumaktion des Experten, einschließlich des separaten Freitags-Cutoffs.
- Die Trailing-Logik folgt der ursprünglichen Steigungsformel (`newStop = Bid - StopLoss - Slope * (Bid - Entry)` für Longs).
- Eigenkapitalbenachrichtigungen aus der Version MQL werden durch Strategieprotokollmeldungen ersetzt.
- Die StockSharp-Implementierung hält ausstehende Aufträge auch dann aktiv, wenn eine Position offen ist, und entspricht damit dem Verhalten der Quelle.

## Nutzungstipps
- Weisen Sie ein Forex-Instrument mit den ordnungsgemäß konfigurierten Werten `PriceStep`, `StepPrice` und `VolumeStep` zu, um eine genaue Größenbestimmung sicherzustellen.
- Kombinieren Sie die Strategie mit StockSharp Risikokontrollen (Portfoliolimits, Schutzmaßnahmen auf Connector-Ebene), wenn Sie live laufen.
- Optimieren Sie `ThresholdPoints`, `TrailingSlope` und `PercentOfProfit` mit Designer oder Runner, um die Breakout-Empfindlichkeit an bestimmte Symbole anzupassen.
