# RobotPowerM5 Meta4 V12 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die RobotPowerM5 Meta4 V12-Strategie ist eine C#-Portierung des MetaTrader 4 Expert Advisors `RobotPowerM5_meta4V12.mq4`. Das Original EA
wurde für Fünf-Minuten-Forex-Charts entwickelt und bewertet das Gleichgewicht zwischen Bulls Power und Bears Power, um zu entscheiden, ob ein neuer
Es sollte eine Long- oder Short-Position eröffnet werden. Die StockSharp-Version behält das Verhalten bei jeweils einer Position bei und reproduziert den Punkt.
basierend auf Stop-Loss-/Take-Profit-Einstellungen und implementiert die Trailing-Stop-Logik neu, die nach und nach Gewinne sichert, sobald der Markt erreicht ist
bewegt sich zugunsten des Handels.

## Handelslogik
1. **Indikatormotor**
   - Fünf-Minuten-Kerzen werden standardmäßig abonniert (der Zeitrahmen ist über den Parameter `CandleType` konfigurierbar).
   - Ein Paar StockSharp-Indikatoren, `BullsPower` und `BearsPower`, werden bei jeder fertigen Kerze mithilfe der konfigurierten aktualisiert
Mittelungszeitraum.
   - Der kombinierte Wert `BullsPower + BearsPower` wird mit einer Verzögerung von einem Takt gespeichert, um die `shift=1`-Aufrufe von zu imitieren
MQL-Code, der immer auf den letzten vollständig geschlossenen Balken wirkt.
2. **Eintrittsregeln**
   - Wenn keine Position offen ist und die verzögerte Summe der Bulls/Bears Power **positiv** ist, wird eine Marktkauforder ausgegeben.
   - Wenn keine Position offen ist und die verzögerte Summe **negativ** ist, wird ein Marktverkaufsauftrag erteilt.
   - Signale werden ignoriert, während eine Position aktiv ist; Der Handel wird ausschließlich über Schutzausgänge abgewickelt.
3. **Volumenhandhabung**
   - Der Parameter `Volume` stellt die angeforderte Losgröße dar. Es wird direkt an `BuyMarket` / `SellMarket` übergeben, wodurch die
Runden Sie den Stecker bei Bedarf auf die Lotstufe des Instruments auf.

## Risikomanagement
- **Stop-Loss** – Der anfängliche Stop wird `StopLossPoints` MetaTrader Punkte vom durchschnittlichen Ausführungspreis entfernt platziert. Das Niveau ist
überwacht mit Kerzentiefs (für Long-Positionen) oder Kerzenhochs (für Short-Positionen); einmal berührt die Strategie Exits am Markt.
- **Take-Profit** – Das Gewinnziel beträgt `TakeProfitPoints` Punkte ab dem Einstieg und wird entsprechend den entsprechenden Kerzenhochs/-tiefs bewertet
wie MT4 Positionen schließt, wenn ein Ziel intrabar getroffen wird.
- **Trailing Stop** – Nachdem sich der Preis um mehr als `TrailingStopPoints` zu Gunsten des Handels bewegt hat, wird ein Trailing Stop aktiviert.
Bei Long-Positionen wird der Stop auf `referencePrice - trailingDistance` verschoben, wobei die Referenz das Maximum der Kerze ist
nah und hoch. Bei Shorts folgt der Stop `referencePrice + trailingDistance` und verwendet dabei das Minimum aus Schluss- und Tiefstkurs der Kerze.
Dies reproduziert das Nachlaufverhalten von EA, das ursprünglich mit `OrderModify` implementiert wurde.

## Parameter
| Name | Beschreibung | Standard | Notizen |
| --- | --- | --- | --- |
| `BullBearPeriod` | Mittelungszeitraum für die Bulls Power- und Bears Power-Indikatoren. | `5` | Durch Erhöhen des Werts wird der Impulsfilter geglättet. |
| `Volume` | Gewünschte Losgröße für jeden Eintrag. | `1` | Das tatsächlich gehandelte Volumen hängt vom Lotschritt und den Limits des Brokers ab. |
| `StopLossPoints` | Anfänglicher Schutzstoppabstand in MetaTrader Punkten. | `45` | Auf `0` setzen, um den harten Stop-Loss zu deaktivieren. |
| `TakeProfitPoints` | Take-Profit-Distanz in MetaTrader Punkten. | `150` | Auf `0` setzen, um ohne festes Gewinnziel zu handeln. |
| `TrailingStopPoints` | Vom Trailing Stop verwendete Distanz, sobald der Handel profitabel ist. | `15` | Auf `0` setzen, um das Nachstellen zu deaktivieren. |
| `CandleType` | Für die Indikatorberechnungen verwendeter Zeitrahmen. | `5m time frame` | Bei Bedarf kann jedes andere `DataType` ausgewählt werden. |

## Implementierungshinweise
- Die Strategie speichert alle Risikostufen (Stop-Loss, Take-Profit, Trailing Stop) intern und gibt Marktausstiege bei Kerzen aus
bestätigen, dass eine Preisschwelle überschritten wurde. Dies spiegelt den MT4-Ansatz wider, bei dem Aufträge Tick für Tick geändert wurden.
- Indikatorabonnements werden über `Subscription.Bind` verkabelt, wodurch sowohl Bulls Power als auch Bears Power in einen einzigen Rückruf eingespeist werden.
- Die Punktgröße wird von `Security.PriceStep` abgeleitet, wodurch die Parameter mit Instrumenten kompatibel bleiben, die in Ticks notieren.
Pips oder Cent.
- Bei Eintrittsprüfungen werden immer die *vorherigen* Indikatorwerte verwendet, um sicherzustellen, dass teilweise gebildete Kerzen niemals Aufträge auslösen.

## Unterschiede zur MQL-Version
- Das Handelsmanagement nutzt explizite Marktaustritte, anstatt die bestehende Stop-Loss-Order zu ändern. Dies ist insgesamt robuster
verschiedene StockSharp-Anschlüsse verwenden und dabei das gleiche Ergebnis erzielen.
- Parameterbereiche werden durch `StrategyParam`-Helfer validiert, sodass ungültige Werte (z. B. negative Trailing Stops) ausgeschlossen sind
zum Zeitpunkt der Konfiguration abgelehnt.
- Detaillierte Protokollierungs-Hooks, Diagrammausgabe und Kerzenabonnements nutzen StockSharps High-Level-API anstelle manueller Tick-Loops.
- Die im MT4-Skript vorhandene Experten-ID-Zeichenfolge ist in StockSharp nicht erforderlich und wurde daher weggelassen.
