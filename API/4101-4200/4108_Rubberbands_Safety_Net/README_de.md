# Rubberbands-Sicherheitsnetzstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

StockSharp Port des Fachberaters RUBBERBANDS 1.6 MetaTrader. Das ursprüngliche System behält ein abgesichertes Paar von Kauf- und Verkaufsscheinen bei, fügt die geschlossene Seite nach jedem Gewinn wieder ein und aktiviert ein Sicherheitsnetz, wenn der laufende Verlust vordefinierte Cash-Grenzwerte überschreitet. Die Konvertierung behält den alternierenden Zyklus bei, passt die Mechanik jedoch an das Nettopositionsmodell von StockSharp an, indem der Durchschnitt in Richtung des aktuellen Engagements gebildet wird, anstatt unabhängige Absicherungsaufträge zu halten.

## Handelslogik

- **Zyklusstart:** Am Ende jeder Minute oder wenn `Enter Now` umgeschaltet wird, eröffnet die Strategie eine Marktposition mit `BaseVolume`. Der nächste Zyklus wechselt die Richtung (kaufen, dann verkaufen, dann wieder kaufen usw.).
- **Grundgewinnziel:** Der laufende nicht realisierte PnL wird mit `TargetProfitPerLot * BaseVolume` verglichen. Bei Erreichen wird die Position liquidiert und der nächste Zyklus ändert die Richtung.
- **Sitzungskontrolle:** `UseSessionTakeProfit` und `UseSessionStopLoss` beobachten den kumulierten realisierten plus nicht realisierten Gewinn, gemessen in Bargeld pro Basislos. Das Erreichen einer der Schwellenwerte löst eine vollständige Liquidation aus und setzt die Zähler zurück.
- **Sicherheitsmodus:** Wenn diese Option aktiviert ist und der nicht realisierte Verlust `SafetyStartPerLot * BaseVolume` übersteigt, wechselt der Algorithmus in den Sicherheitsmodus und beginnt mit der Mittelung in der aktuellen Richtung, indem er zusätzliche Aufträge der Größe `SafetyVolume` sendet. Jeder zusätzliche Verlust von `SafetyStepPerLot` pro Sicherheitslos führt zu einer weiteren Mittelungsreihenfolge.
- **Sicherheitsausgänge:** Im Sicherheitsmodus wird die Position abgeflacht, sobald der nicht realisierte Gewinn `SafetyProfitPerLot * |Position|` erreicht oder wenn die Sitzungsebenenmetrik `SafetyModeTakeProfitPerLot * BaseVolume` überschreitet.

## Teilnahmebedingungen

### Lange Einträge
- Keine offene Belichtung und entweder die Minute, die gerade verschoben wurde, oder `Enter Now` ist wahr.
- Die Strategie geht derzeit von einer langen Eröffnung aus (Zyklen wechseln sich ab).
- Der manuelle Stoppschalter ist deaktiviert.

### Kurze Einträge
- Identisch mit den langen Bedingungen, aber die nächste Zyklusrichtung ist kurz.

## Exit-Management

- **Basiszieltreffer:** Schließen Sie die gesamte Position und kehren Sie die Zyklusrichtung um.
- **Sitzung TP/SL:** Schließen Sie die Position, löschen Sie die Zähler für realisierte Gewinne und bleiben Sie bis zum Trigger in der nächsten Minute unverändert.
- **Sicherheitsgewinn:** Schließen Sie die Position, wenn das Netto-PnL-Ziel erreicht wird, während der Sicherheitsmodus aktiv ist.
- **Sicherheitsdurchschnitt:** Zusätzliche Sicherheitsanordnungen werden angehängt, wenn der nicht realisierte Verlust in Schritten von `SafetyStepPerLot` wächst.
- **Manuelle Schließung:** Durch die Einstellung `Close Now` wird die Position bei der nächsten Kerze geschlossen und der realisierte Gewinnakkumulator zurückgesetzt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `BaseVolume` | Market-Order-Größe für den primären Zweig. |
| `TargetProfitPerLot` | Gewinnziel (Cash pro Lot) für den Basishandel. |
| `UseSessionTakeProfit` / `SessionTakeProfitPerLot` | Aktivieren und konfigurieren Sie den sitzungsweiten Take-Profit. |
| `UseSessionStopLoss` / `SessionStopLossPerLot` | Aktivieren und konfigurieren Sie den sitzungsweiten Stop-Loss. |
| `UseSafetyMode` | Schalten Sie die Sicherheitsmittelungslogik um. |
| `SafetyStartPerLot` | Verlust pro Basislos, der den Sicherheitsmodus aktiviert. |
| `SafetyVolume` | Volumen jedes Sicherheitsdurchschnittsauftrags. |
| `SafetyStepPerLot` | Zusätzlicher Verlust pro Sicherheitslos, der erforderlich ist, um einen weiteren Sicherheitsauftrag in die Warteschlange zu stellen. |
| `SafetyProfitPerLot` | Im Sicherheitsmodus wird das Gewinnziel angewendet. |
| `SafetyModeTakeProfitPerLot` | Gewinnziel auf Sitzungsebene, während der Sicherheitsmodus aktiv ist. |
| `UseInitialState`, `InitialProfitSoFar`, `InitialSafetyMode`, `InitialSafetyToBuy`, `InitialUsedSafetyCount` | Staatliche Wiederherstellungshelfer für Neustarts. |
| `QuiesceNow`, `Enter Now`, `Stop Trading`, `Close Now` | Manuelle Schalter, die die ursprünglichen externen EA-Variablen widerspiegeln. |
| `CandleType` | Zeitrahmen der Kerzenserie, die die Schleife antreibt (Standard 1 Minute). |

## Praktische Hinweise

- StockSharp behält eine einzelne Nettoposition pro Instrument. Anstatt gleichzeitig Kauf- und Verkaufstickets zu halten, wird der Umrechnungsdurchschnitt in die bestehende Position umgewandelt, wenn der Sicherheitsmodus aktiv ist. Dadurch bleiben die bargeldbasierten Schwellenwerte erhalten und gleichzeitig das Netting-Modell eingehalten.
- Die Gewinn- und Verlustschwellen werden in der Kontowährung pro Los ausgedrückt und spiegeln die externen Eingaben von MetaTrader wider. Passen Sie sie an den Tick-Wert des Instruments an.
- Manuelle Schalter (`Stop Trading`, `Close Now`, `Enter Now`, `Quiesce`) können im Handumdrehen über die Benutzeroberfläche geändert werden, um die Strategie zu steuern, ohne den Code bearbeiten zu müssen.
- `StartProtection()` wird beim Start aufgerufen, um das standardmäßige StockSharp-Schutz-Framework für Risikokontrollen wiederzuverwenden.
- Stellen Sie sicher, dass die Metadaten des Instruments (`VolumeStep`, `VolumeMin`, `VolumeMax`) so konfiguriert sind, dass die angeforderten Volumes die Austauschvalidierung bestehen. Der Helfer richtet sie automatisch auf den nächstgelegenen gültigen Schritt aus.
