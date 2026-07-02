# EXP FIBO ZZ Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die EXP FIBO ZZ-Strategie ist eine C#-Portierung des MetaTrader 4 Expert Advisors `EXP_FIBO_ZZ_V1en`. Es reproduziert den ursprünglichen Ausbruch
Logik: Überwachen Sie den letzten bestätigten ZigZag-Korridor, platzieren Sie einen Kaufstopp über dem Swing-Hoch und einen Verkaufsstopp unter dem Swing-Tief und
Fügen Sie Fibonacci-basierte Stop-Loss- und Take-Profit-Orders hinzu. Die StockSharp-Version macht alle konfigurierbaren Eingaben verfügbar
`StrategyParam`-Objekte, fügt eine umfassende Validierung hinzu und behält die ursprünglichen Geldverwaltungsoptionen einschließlich des saldenbasierten Risikos bei
Dimensionierung und die Break-Even-Stopp-Einstellung.

## Handelslogik
1. **Datenvorbereitung**
   - Die Strategie abonniert die konfigurierten `CandleType` (Standard: 1-Minuten-Kerzen) und speist die Serie in `Highest` und ein
`Lowest`-Indikatoren mit einer Länge gleich `ZigZagDepth`.
   - Ein leichter ZigZag-Detektor verfolgt die letzten drei Pivot-Preise. Ein neuer Pivot wird nur registriert, wenn:
     * Das Hoch/Tief der Kerze entspricht der Ausgabe des Indikators.
     * Seit dem letzten Wendepunkt sind mindestens `ZigZagBackstep` Balken vergangen.
     * Die Preisabweichung vom aktuellen Pivot überschreitet `ZigZagDeviationPips` (ausgedrückt in MetaTrader Pips).

2. **Korridorvalidierung**
   - Sobald drei Drehpunkte verfügbar sind, definieren die beiden ältesten den Korridor. Der Handel wird nur fortgesetzt, wenn die Korridorhöhe dazwischen liegt
`MinCorridorPips` und `MaxCorridorPips` und der neueste Pivot liegen streng innerhalb des Bandes mit einem kleinen Puffer im Broker-Stil.
   - Außerhalb des vom Benutzer festgelegten Handelsfensters (`StartHour/StartMinute` bis `StopHour/StopMinute`) werden alle ausstehenden Aufträge storniert.

3. **Auftragserteilung**
   - Kauf- und Verkaufsstopppreise werden als Korridorgrenzen plus/minus `EntryOffsetPips` berechnet.
   - Die Stop-Loss-Distanz beträgt `corridor * FiboStopLoss / 100`. Die Take-Profit-Distanz folgt der Formel MetaTrader
`corridor * (FiboTakeProfit / 100 - 1)` mit auf Null begrenzten negativen Werten.
   - Vor der Auftragserteilung berechnet die Strategie das Handelsvolumen. Wenn `RiskPercent > 0`, multipliziert der Code das ausgewählte Kapital
Quelle (Eigenkapital, wenn `UseBalanceForRisk` gleich `true` ist, andernfalls Eigenkapital minus gesperrte Marge) durch den Risikoprozentsatz und dividiert
das Ergebnis um den Referenzpreis. Das Volumen wird an das Börsenlotraster angepasst und auf die Börsenlimits begrenzt. Wann
Wenn die erforderlichen Informationen nicht verfügbar sind, greift der Algorithmus auf `FixedVolume` zurück.
   - Aktive Einstiegsaufträge werden immer dann geändert, wenn sich der Zielpreis oder das Volumen ändert; andernfalls werden neue Bestellungen aufgegeben.

4. **Positionsverwaltung**
   - Sobald eine Position eröffnet wird, storniert der Algorithmus die entgegengesetzte ausstehende Order und registriert schützende Orders:
     * Stop-Loss über `SellStop`/`BuyStop` im vorberechneten Abstand.
     * Optionaler Take-Profit über `SellLimit`/`BuyLimit`.
   - Das optionale Break-Even-Modul (`EnableBreakEven`) spiegelt die ursprüngliche `MovingInWL`-Routine wider. Nach dem Ansammeln
`BreakEvenTriggerPips` des Gewinns wird der Stop auf den Einstiegspreis plus/minus `BreakEvenOffsetPips` verschoben, was mindestens garantiert
einen winzigen Gewinn und verhindert gleichzeitig wiederholte Anpassungen.

5. **Sitzungswartung**
   - Durch das Verlassen des Handelsfensters oder das Abflachen der Position werden alle ausstehenden ausstehenden Aufträge oder Schutzaufträge storniert. Die Methode
`OnStopped` löscht außerdem jede Bestellung, wenn die Strategie endet.

## Parameter
| Name | Beschreibung | Standard | Notizen |
| --- | --- | --- | --- |
| `CandleType` | Datenreihen, die zum Erstellen der ZigZag-Pivots verwendet werden. | `1m TimeFrame()` | Unterstützt jeden Kerzentyp StockSharp. |
| `ZigZagDepth` | Mindestanzahl von Kerzen zwischen ZigZag-Schwüngen. | `12` | Entspricht der MT4-Eingabe `ExtDepth`. |
| `ZigZagDeviationPips` | Mindestabweichung (in MetaTrader Pips), bevor ein neuer Pivot akzeptiert wird. | `5` | Spiegelt `ExtDeviation`. |
| `ZigZagBackstep` | Mindestanzahl der Balken, bevor sich der ZigZag wieder umkehren kann. | `3` | Entspricht `ExtBackstep`. |
| `EntryOffsetPips` | Abstand in Pips, der bei der Platzierung von Stop-Orders über/unter dem Korridor hinzugefügt wird. | `5` | Spiegelt `n_pips`. |
| `MinCorridorPips` | Untergrenze für die Korridorgröße. | `20` | Spiegelt `Min_Corridor`. |
| `MaxCorridorPips` | Obergrenze für die Korridorgröße. | `100` | Spiegelt `Max_Corridor`. |
| `FiboStopLoss` | Fibonacci-Verhältnis, das auf den Korridor angewendet wird, um die Stop-Loss-Distanz abzuleiten. | `61.8` | Spiegelt `Fibo_StopLoss`. |
| `FiboTakeProfit` | Fibonacci-Verhältnis zur Berechnung des Take-Profit-Ziels. | `161.8` | Spiegelt `Fibo_TakeProfit`. |
| `StartHour` / `StartMinute` | Beginn der erlaubten Handelssitzung. | `00:01` | Bestellungen werden außerhalb des Fensters storniert. |
| `StopHour` / `StopMinute` | Ende der Handelssitzung. | `23:59` | Unterstützt Nachtsitzungen, die um Mitternacht beginnen. |
| `UseBalanceForRisk` | Wählen Sie Eigenkapital (`true`) oder verfügbares Bargeld (`false`) für die Risikogröße. | `true` | Spiegelt `Choice_method`. |
| `RiskPercent` | Anteil des Kapitals, der dem nächsten Trade zugewiesen wird. | `1` | Auf `0` setzen, um die risikobasierte Größenanpassung zu deaktivieren. |
| `FixedVolume` | Losgröße, die verwendet wird, wenn die Risikogrößenbestimmung deaktiviert oder nicht verfügbar ist. | `0.1` | Spiegelt die Eingabe `Lots`. |
| `EnableBreakEven` | Aktiviert die Break-Even-Stopp-Anpassung. | `true` | Spiegelt `MovingInWL`. |
| `BreakEvenTriggerPips` | Gewinn in Pips erforderlich, bevor der Stopp verschoben wird. | `13` | Spiegelt `LevelProfit`. |
| `BreakEvenOffsetPips` | Auf den Break-Even-Stopp angewendeter Offset in Pips. | `2` | Spiegelt `LevelWLoss`. |
| `DrawCorridorLevels` | Zeichnen Sie den aktiven Korridor in das Diagramm ein. | `false` | Spiegelt das optionale Strichzeichnungsflag. |

## Implementierungshinweise
- Bei der Pip-Konvertierung werden die MetaTrader-Konventionen berücksichtigt, indem der `PriceStep` für drei- und fünfstellige Forex-Symbole mit 10 multipliziert wird.
- Bestellpreise und -volumina werden mithilfe der Börsenmetadaten (`PriceStep`, `VolumeStep`,
`MinVolume`, `MaxVolume`).
- Wenn Portfoliodaten oder Referenzpreise fehlen, wird die Risikodimensionierung sanft zurückgesetzt, um sicherzustellen, dass die Strategie weiterhin funktioniert
das konfigurierte Festlos.
- Die Break-Even-Routine hebt den Schutzstopp nur einmal pro Trade auf und registriert ihn erneut. Der Stop wird nie darüber hinaus platziert
Eintrittspreis.
- Wenn `DrawCorridorLevels` aktiviert ist, zeichnet die Strategie ein vertikales Segment zwischen den oberen und unteren Pivotpunkten des Stroms
Korridor, der eine schnelle visuelle Bestätigung der Handelsspanne ermöglicht.

## Unterschiede zur MetaTrader-Version
- Diagrammobjekte, Sounds und Bildschirmkommentare aus dem MT4-Skript wurden weggelassen; StockSharp Protokollierung und Diagrammprimitive decken das ab
gleiche Bedürfnisse.
- Bei der Risikodimensionierung werden Portfolio-Eigenkapital und letzte bekannte Preise anstelle von `MarketInfo` Margin-Werten verwendet, da es sich bei diesen Details um Maklerdaten handelt
spezifisch und plattformunabhängig nicht verfügbar.
- Die Auftragsverwaltung verwendet das übergeordnete StockSharp API (`BuyStop`, `SellStop`, `SellLimit`, `BuyLimit`) anstelle eines manuellen Tickets
Handhabung. Das Verhalten bleibt gleich, erfordert jedoch weniger Boilerplate-Code.
- Der ZigZag-Detektor implementiert die Tiefen-/Abweichungs-/Rückschrittlogik mit integrierten Indikatoren neu, um die Kompatibilität zu gewährleisten
Das Streaming-Kerzenmodell von StockSharp.
