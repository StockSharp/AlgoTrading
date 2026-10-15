# Kanal EA Limit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- **Ursprung**: Konvertierung des MetaTrader 5 Experts `ChannelEA1.mq5`.
- **Zweck**: Überwachung eines Intraday-Preiskanals zwischen zwei benutzerdefinierten Stunden und Platzierung von Limit-Orders am Ende dieses Fensters.
- **Ansatz**: Die Strategie verfolgt die höchsten und niedrigsten Preise, die während der Sitzung beobachtet wurden, und platziert symmetrische Limit-Orders, um potenzielle Umkehrungen zurück zur gegenüberliegenden Kanalseite zu handeln.

Die Strategie eignet sich für Symbole, die nach Etablierung einer Tagesspanne Mean Reversion aufweisen. Das Design arbeitet auf Netting-Konten: Eine ausgeführte Verkaufs-Limit-Order schließt eine bestehende Long-Position, bevor eine neue Short-Position eröffnet wird, und umgekehrt.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `BeginHour` | `1` | Stunde (0-23), zu der das Intraday-Range-Tracking beginnt. Die Strategie storniert ausstehende Orders und schließt Positionen zu diesem Zeitpunkt. |
| `EndHour` | `10` | Stunde (0-23), zu der die akkumulierte Range ausgewertet und neue Limit-Orders platziert werden. Unterstützt Overnight-Sitzungen: Wenn `BeginHour > EndHour`, erstreckt sich die Sitzung über Mitternacht. |
| `OrderVolume` | `1` | Volumen, das auf jede ausstehende Order angewendet wird. |
| `CandleType` | `1 Stunde` Zeitrahmen | Kerzenreihe, die zum Aufbau des Kanals verwendet wird. Sie können zu jedem von StockSharp unterstützten Zeitrahmen wechseln. |

## Handelslogik
1. **Sitzungsverwaltung**
   - Die Strategie leitet die Sitzungsstart- und Endtimestamps aus den `BeginHour`- und `EndHour`-Parametern unter Verwendung der Kerzen-Timestamps ab. Wenn `BeginHour > EndHour`, wird das Ende auf den nächsten Tag verschoben.
   - Bei der ersten fertigen Kerze, deren Schlusskurs die Startgrenze erreicht, storniert die Strategie alle aktiven Orders, schließt die offene Position und setzt die Sitzungsstatistik zurück.
2. **Kanalaufbau**
   - Nur Kerzen, deren Eröffnungszeit innerhalb des Sitzungsfensters liegt, tragen zur Range bei. Die Strategie pflegt das laufende Maximum-Hoch und Minimum-Tief für die Sitzung und zählt die Anzahl der beitragenden Kerzen.
   - Mindestens zwei fertige Kerzen sind erforderlich, um eine gültige Range zu bilden, was das Verhalten des ursprünglichen MQL5-Experts widerspiegelt (Bedingung `n > 2`).
3. **Order-Platzierung am Sitzungsende**
   - Wenn eine fertige Kerze die Endgrenze überschreitet, prüft die Strategie, ob die Range gebildet wurde und ob das Tief strikt unter dem Hoch liegt.
   - Dann werden zwei ausstehende Orders platziert:
     - `BuyLimit` am aufgezeichneten Sitzungstief mit `OrderVolume`-Volumen.
     - `SellLimit` am aufgezeichneten Sitzungshoch mit demselben Volumen.
   - Orders bleiben aktiv, bis die nächste Sitzung beginnt. Da die Strategie auf einem Netting-Konto läuft, dienen diese Orders sowohl als Einstiege als auch als Ausstiege: Zum Beispiel schließt der `SellLimit` eine bestehende Long-Position am Sitzungshoch, bevor eine neue Short-Position etabliert wird.
4. **Vorbereitung der nächsten Sitzung**
   - An der nächsten Startgrenze schließt die Strategie alle verbleibenden Positionen und entfernt übrig gebliebene ausstehende Orders, bevor der neue Kanal gemessen wird.

## Zusätzliche Hinweise
- Es wird kein expliziter Stop-Loss gesetzt. Risikomanagement muss durch Positionsgrößen, manuelle Eingriffe oder externe Schutzlogik gesteuert werden.
- Die Logik verwendet nur fertige Kerzen (`CandleStates.Finished`), um sich am ursprünglichen EA-Verhalten auszurichten.
- Stellen Sie sicher, dass die Zeitzone des Datenfeeds und des Servers Ihren Erwartungen entspricht, da Sitzungsgrenzen in Börsen-/Ortszeit bewertet werden.
- Berücksichtigen Sie bei der Optimierung sowohl die Handelszeiten als auch die Kerzendauer; die Strategie ist sensibel gegenüber der Kombination, da die aufgezeichnete Range vom ausgewählten Zeitrahmen abhängt.
