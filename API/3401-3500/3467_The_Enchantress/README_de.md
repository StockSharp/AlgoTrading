# Die Enchantress-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Enchantress-Strategie repliziert das Selbstlernverhalten des gleichnamigen Expertenberaters MQL4. Das Original EA
klassifiziert jede fertige Kerze in zehn Eimer, führt eine fortlaufende Historie der letzten sieben Eimer und startet den „virtuellen“ Kauf
und Verkaufsaufträge für jedes neue Sieben-Kerzen-Muster. Immer wenn der Preis später die virtuellen Take-Profit- oder Stop-Loss-Niveaus berührt, wird der
Muster erhält eine positive oder negative Bewertung. Live-Trades werden nur ausgelöst, wenn das aktuelle Sieben-Kerzen-Muster dazu gehört
leistungsstärkste virtuelle Muster. Dieser StockSharp-Port bewahrt diese Rückkopplungsschleife und stellt alle wichtigen Konfigurationsoptionen zur Verfügung
als Strategieparameter.

## Kerzenklassifizierung

1. Jede fertige Kerze wird einmal anhand ihrer Eröffnungs-, Schluss-, Höchst- und Tiefstkurse bewertet.
2. Die Körperrichtung unterteilt Kerzen in bärische (Ziffern `0–4`) und bullische (Ziffern `5–9`).
3. Das Hoch/Niedrig-Verhältnis `100 - Low * 100 / High` bestimmt die genaue Ziffer innerhalb jeder Gruppe:
   - `0/5` für sehr kleine Bereiche (≤ 0,04)
   - `1/6` für kleine Bereiche (0,04 – 0,15)
   - `2/7` für mittlere Bereiche (0,15 – 0,25)
   - `3/8` für weite Bereiche (0,25 – 0,40)
   - `4/9` für extrem weite Bereiche (> 0,40)
4. Die neueste Ziffer wird an das rollierende Fenster mit sieben Zeichen angehängt, das das aktuelle Marktmuster darstellt.

Diese Klassifizierung entspricht den numerischen Buckets, die von der `ManagePatterns`-Routine des ursprünglichen EA erstellt wurden.

## Virtuelle Bestellmaschine

- Sobald sieben Ziffern verfügbar sind, erstellt die Strategie einen gepaarten Satz virtueller Orders (Long und Short) für das aktive Muster.
- Der virtuelle Einstiegspreis entspricht dem Schlusskurs der Kerze. Virtuelle Stopps und Ziele werden aus `VirtualStopLoss` und abgeleitet
`VirtualTakeProfit` mit der Instrumentenpreisstufe.
- Bei nachfolgenden Kerzen prüft die Strategie, ob das Hoch/Tief der Kerze die virtuellen Ziele berührt oder stoppt:
  - Ein Zieltreffer trägt `+1` zum jeweiligen bullischen oder bärischen Score bei.
  - Ein Stopptreffer trägt `-3` zum jeweiligen Punktestand bei und reproduziert den vom EA verwendeten Strafstoß.
- Geschlossene virtuelle Aufträge werden verworfen, um die Speichernutzung begrenzt zu halten, während die akkumulierten Punkte mit ihnen verknüpft bleiben
siebenstelliger Musterschlüssel.

## Signalerzeugung

Vor der Verarbeitung der nächsten Kerze prüft die Strategie das aktuelle siebenstellige Muster (das nur aus vergangenen Kerzen erstellt wird). Handel ist
erlaubt von Montag bis Donnerstag; Freitage werden genau wie in der MQL-Version übersprungen. Es gelten folgende Regeln:

1. Erstellen Sie die zehn besten bullischen und bärischen Muster nach Punktzahl (nur Punktzahlen ≥ 1 werden berücksichtigt).
2. Wenn das aktuelle Muster zur bullischen Leitlinie gehört, tätigen Sie einen Marktkauf. Wenn es zur Gruppe der bärischen Anführer gehört, platzieren Sie a
Marktverkauf. Die gleiche Kerze kann nicht zwei Einträge auslösen, da die Strategie den Zeitstempel der Kerze nach der ersten Füllung aufzeichnet.
3. Nach jeder Entscheidung wird die frisch abgeschlossene Kerze an das Musterfenster und die virtuellen Aufträge für das neue Muster angehängt
werden ins Leben gerufen.

## Schutzanordnungen und Größenbestimmung

- Echte Trades verwenden die Distanzen `StopLoss` und `TakeProfit`, ausgedrückt in Pips. Beide Parameter werden über in Preisunterschiede übersetzt
Der Wertpapierpreisschritt wird bis `SetStopLoss`/`SetTakeProfit` angewendet, direkt nachdem die Marktorder ausgeführt wurde.
- Die Positionsgrößenbestimmung kann in zwei Modi erfolgen:
  - **Festes Los**: `LotSize` wird wörtlich verwendet (angepasst an die Einschränkungen hinsichtlich Schritt-/Mindest-/Höchstvolumen des Austauschvolumens).
  - **Risikogeldmanagement**: Das Volumen beträgt `PortfolioValue / 100000 * RiskPercent`. Dies spiegelt das Original `AccountFreeMargin` wider.
Formel und greift auf die Festmenge zurück, wenn kein Portfoliowert verfügbar ist.

## Parameter

| Name | Beschreibung | Standard |
|------|-------------|---------|
| `LotSize` | Feste Ordergröße, wenn die Geldverwaltung deaktiviert ist. | `0.01` |
| `UseRiskMoneyManagement` | Schalten Sie den dynamischen Größenblock um. | `true` |
| `RiskPercent` | Prozentsatz des Portfoliowerts, der im Risikomodus verwendet wird. | `15` |
| `StopLoss` | Echte Stop-Loss-Distanz in Pips. | `60` |
| `VirtualStopLoss` | Stoppdistanz, die für die virtuelle Wertung verwendet wird. | `55` |
| `TakeProfit` | Echte Take-Profit-Distanz in Pips. | `19` |
| `VirtualTakeProfit` | Take-Profit-Distanz für virtuelle Wertung. | `25` |
| `CandleType` | Zeitrahmen der verarbeiteten Kerzen. | `5m` |

## Nutzungshinweise

- Stellen Sie sicher, dass die Sicherheitsmetadaten (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`) ausgefüllt sind. Ansonsten Größe und Pips
Bei Konvertierungen wird auf generische Standardwerte zurückgegriffen.
- Damit die risikobasierte Größenbestimmung funktioniert, muss die Portfoliobewertung (`Portfolio.CurrentValue` oder `Portfolio.BeginValue`) verfügbar sein.
- Die Strategie gilt nur für fertige Kerzen und führt keine virtuellen Auftragsprüfungen innerhalb der Bar durch. Der Hoch/Tief-Vergleich ist der
größte Annäherung an die Tick-basierte Logik von MT4.
- Um die Musterdatenbank schneller aufzuwärmen, führen Sie die Strategie auf historischen Daten im Backtesting-Modus aus – die Bewertungslogik ist in identisch
sowohl Simulation als auch Live-Handel.
