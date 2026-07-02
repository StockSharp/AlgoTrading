# GoldWarrior02b-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Umfassender StockSharp-Port des MetaTrader 4-Expertenberaters *GoldWarrior02b* (Ordner `MQL/7694`).
Es kombiniert einen Commodity Channel Index (CCI), einen benutzerdefinierten Impulsmesser und einen handgefertigten ZigZag-Swing-Detektor
und wertet Signale nur wenige Sekunden vor jeder 15-Minuten-Grenze aus. Das Ziel dieser Übersetzung ist
um die High-Level-Logik des ursprünglichen Roboters nachzuahmen und dabei das Nettopositionsausführungsmodell von StockSharp zu respektieren.

## Hauptmerkmale

- **Impulsfilter** – ersetzt den benutzerdefinierten Indikator `DayImpuls` durch Mittelung des Abstands zum Öffnen/Schließen der Kerze
normalisiert durch den Preisschritt des Instruments.
- **Zick-Zack-Struktur** – stellt die jüngsten Swing-Hochs und -Tiefs wieder her, um festzustellen, ob der Markt einen Auf- oder Abwärtstrend aufweist.
- **Timing-Gate** – Eingaben sind nur zulässig, wenn die aktuelle Kerze während der letzten 15 Sekunden der Minuten 14, 29, 44 oder 59 schließt.
- **Risikokontrollen** – umfasst Stop-Loss, Take-Profit, Trailing-Stop (optional) und ein kontoweites, gemessenes Gewinnziel
in Währungseinheiten. Die Standardwerte spiegeln die MetaTrader-Eingaben wider (Stop bei 1.000 Punkten, Take-Profit bei 150 Punkten, Trailing deaktiviert).
- **Nettorisiko** – StockSharp behält eine einzige Nettoposition pro Wertpapier, also die mehrstufige Absicherung und Lotskalierung
aus der MQL-Implementierung werden nicht reproduziert. Stattdessen konzentriert sich die Strategie auf ein einziges Einstiegsvolumen.

## Handelslogik

### Signalvorbereitung

1. Abonnieren Sie Kerzen, die durch `CandleType` definiert sind (standardmäßig 5 Minuten Zeitrahmen).
2. Berechnen Sie CCI und den Impulsdurchschnitt mithilfe des gemeinsamen `ImpulsePeriod` (Standard 21 Balken).
3. Aktualisieren Sie die ZigZag-Schwungrichtung, sobald die Abweichung `ZigZagDeviation` Punkte und die Tiefe/den Rückschritt überschreitet
Einschränkungen erfüllt sind.
4. Speichern Sie die vorherigen Werte der Indikatoren, um die „aktuellen“ (`cci0`, `imp`) und „vorherigen“ (`cci1`, `nimp`) zu replizieren.
Puffer, die im Expert Advisor verwendet werden.

### Teilnahmebedingungen

Ein Aufbau wird nur dann ausgewertet, wenn aktuell keine Position offen ist, seit dem letzten Ausstieg mindestens 15 Sekunden vergangen sind und
`AllowEntryTime` gibt `true` zurück (Ende des 15-Minuten-Blocks).

**Lang:**
- Der jüngste ZigZag-Schwung zeigt nach unten (neues Tief niedriger als das vorherige).
- Entweder
  - Der aktuelle CCI steigt im Vergleich zum vorherigen Balken, der vorherige CCI liegt unter -50, der aktuelle CCI bleibt unter -30,
der Impuls wird positiv und der vorherige Impuls war negativ; oder
  - aktueller CCI liegt unter -200, der vorherige CCI war noch niedriger, der Impuls bleibt unter `ImpulseBuyThreshold`
und ist stärker als der vorherige Impuls.

**Kurz:**
- Der jüngste ZigZag-Schwung zeigt nach oben (neues Hoch höher als das vorherige).
- Entweder
  - Der aktuelle CCI sinkt im Vergleich zum vorherigen Balken, der vorherige CCI liegt über 50, der aktuelle CCI bleibt über 30,
der Impuls wird negativ und der vorherige Impuls war positiv; oder
  - Der aktuelle CCI liegt über 200, der vorherige CCI war höher, der Impuls bleibt über `ImpulseSellThreshold`
und ist schwächer als der vorherige Impuls.

Liegt der vorherige Impulswert zwischen `ImpulseSellThreshold` und `ImpulseBuyThreshold`, wird das Signal ignoriert.

### Exit-Management

- **Stop-Loss** – wird ausgelöst, wenn der Preis `StopLossPoints` über den Einstiegspreis hinausgeht (standardmäßig 1.000 Punkte).
- **Take-Profit** – schließt die Position nach Erreichen von `TakeProfitPoints` (150 Punkte).
- **Trailing Stop** – optional; Wenn aktiviert, wird es nach Preisbewegungen aktiviert `TrailingStopPoints + TrailingStepPoints`
zugunsten der Position und liegt dann um `TrailingStopPoints` hinter dem Preis zurück.
- **Gewinnziel** – rechnet den offenen PnL mit `PriceStep` und `StepPrice` in die Kontowährung um
schließt die Position, sobald sie `ProfitTarget` überschreitet (Standard 300).

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `BaseVolume` | Handelsgröße für Einträge. | `0.1` |
| `StopLossPoints` | Stoppdistanz in Punkten. | `1000` |
| `TakeProfitPoints` | Take-Profit-Distanz in Punkten. | `150` |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Punkten (0 deaktiviert das Trailing). | `0` |
| `TrailingStepPoints` | Zusätzlicher Abstand vor der Aktivierung des Nachlaufs. | `0` |
| `ImpulsePeriod` | Zeitraum sowohl für CCI als auch für Impulsberechnungen. | `21` |
| `ZigZagDepth` | Mindestbalken zwischen neuen ZigZag-Schwüngen. | `12` |
| `ZigZagDeviation` | Minimale Preisbewegung (in Punkten) zur Bestätigung eines Swings. | `5` |
| `ZigZagBackstep` | Mindestbalken, bevor ein neuer Schwung akzeptiert wird. | `3` |
| `ProfitTarget` | Schwelle für nicht realisierten Gewinn (Kontowährung). | `300` |
| `ImpulseSellThreshold` | Mindestimpulswert für Kurzschlüsse erforderlich. | `-30` |
| `ImpulseBuyThreshold` | Maximal zulässiger Impulswert für Long-Positionen. | `30` |
| `CandleType` | Arbeitszeitrahmen. | `5 minute time frame` |

## Unterschiede zum ursprünglichen Expert Advisor

- Die Version MetaTrader verwendet `GlobalVariableSet` zur Ratenbegrenzung von Bestellungen und speichert die Ticketanzahl für Absicherungsraster.
Dieser Port behält die zeitbasierte Drosselung bei, jedoch nicht die Durchschnitts-/Absicherungsleiter, da StockSharp Konten
werden verrechnet.
- Die Auftragsverwaltung erfolgt über Marktaufträge (`BuyMarket`, `SellMarket`), um innerhalb der allgemeinen API-Richtlinien zu bleiben.
- Die Impulsberechnung wird vereinfacht; Das Original `DayImpuls` stellt zwei Puffer bereit (`imp`, `nimp`). Hier beide Puffer
werden durch die aktuellen und vorherigen gleitenden Durchschnittswerte angenähert.

## Nutzungstipps

- Konfigurieren Sie `CandleType` so, dass es dem bei der Optimierung verwendeten Zeitrahmen entspricht (das Original EA funktioniert auf M5).
- Stellen Sie sicher, dass das Instrument `PriceStep`- und `StepPrice`-Metadaten bereitstellt, um Punktabstände korrekt umzuwandeln.
- Backtest mit realistischer Slippage/Latenz, um zu bestätigen, dass sich das Eingangstor (letzte Sekunden vor der Viertelstunde) wie erwartet verhält.

## Haftungsausschluss

Diese Strategie wird zu Bildungszwecken bereitgestellt. Führen Sie vorher gründliche Tests mit historischen und Forward-Daten durch
echtes Kapital riskieren.
