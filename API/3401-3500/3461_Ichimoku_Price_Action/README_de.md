# Ichimoku Preisaktionsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Ichimoku Price Action Strategy** ist ein zeitgefiltertes MACD-Momentum-System, das vom MQL4-Experten „Ichimoku Price Action Strategy v1.0“ in die StockSharp-Hochebene API portiert wurde. Der ursprüngliche EA öffnete Marktaufträge, wann immer der Handel für das Instrument aktiviert war, und der optionale MACD-Filter bestätigte die Richtung. Dieser C#-Port behält die gleiche Idee bei und bietet gleichzeitig detaillierte Risikokontrollen für Stop-Loss-Platzierung, Break-Even-Handhabung und Trailing-Exits.

Die Strategie ist für diskretionäre Händler konzipiert, die ein tageszeitbezogenes Richtungsspiel mit minimalen Indikatorabhängigkeiten automatisieren möchten. Alle Handelssignale werden auf abgeschlossenen Kerzen des gewählten Handelszeitrahmens ausgewertet, wobei Hilfszeitrahmen für ATR- und Swing-basierte Schutzstopps unterstützt werden.

> **Wichtig:** Die StockSharp-Version verwaltet jeweils höchstens eine Nettoposition. Ein gleichzeitiges Long/Short-Engagement im Hedge-Stil aus der Originalvorlage wird nicht unterstützt, da StockSharp `Strategy` auf Nettopositionen angewendet wird. Alle anderen Geldverwaltungsfunktionen werden durch Stopp-, Ziel- und Trailing-Logik ausgedrückt, die bei jeder fertigen Kerze ausgeführt wird.

## Handelslogik
1. **Sitzungsfilter** – Einträge sind nur zulässig, wenn die aktuelle Tageszeit innerhalb des `[StartTime; EndTime]`-Fensters liegt. Wenn Sie beide Parameter auf `00:00` setzen, wird der Sitzungsfilter deaktiviert.
2. **MACD-Bestätigung (optional)** – Bei `UseMacdFilter = true` erfordern Long-Positionen eine MACD-Hauptlinie über der Signallinie, Short-Positionen erfordern das Gegenteil. MACD-Einstellungen sind vollständig konfigurierbar.
3. **Auftragserteilung** – Wenn der Handel für eine Richtung aktiviert ist und keine Position offen ist, sendet die Strategie einen Marktauftrag mit dem konfigurierten `Volume`.
4. **Schutzstopps** – Abhängig von `StopLossMode` wird der anfängliche Stopp mithilfe einer festen Pip-Distanz, eines ATR-Vielfachen oder des letzten Swing-Extrems aus einem niedrigeren Zeitrahmen platziert. Der Stop wird bei jeder Kerze neu berechnet und verschärft, wenn das neu berechnete Niveau konservativer ist.
5. **Ziele** – Ein festes Pip-Ziel oder ein dynamisches Risiko-/Ertragsziel basierend auf dem aktiven Stopp wird bei jeder Kerze überprüft. Sobald dieser erreicht ist, wird die Position zum Marktwert geschlossen.
6. **Break-Even und Trailing** – Wenn der nicht realisierte Gewinn `MoveToBreakEven` erreicht, wird der Stop auf den Einstiegspreis gezogen. Nach `TrailingTrigger` Pips Gewinn wird das Trailing-Modul aktiviert und drückt den Stop jedes Mal weiter, wenn sich der Preis um `TrailingStep` Pips erhöht, während ein Abstand von `TrailingStop` Pips vom Kerzenschluss eingehalten wird.
7. **Reverse Exit** – Bei `CloseOnReverse = true` schließt jedes entgegengesetzte Eingangssignal sofort die aktuelle Position, bevor es möglicherweise in die neue Richtung wechselt.

## Risikomanagement
- **Stop-Loss**
  - *Feste Pips* – Verwendet `StopLossPips` multipliziert mit der Preisstufe des Instruments.
  - *ATR-Multiplikator* – Verwendet den neuesten ATR-Wert von `AtrCandleType` multipliziert mit `AtrMultiplier`.
  - *Swing hoch/tief* – Verwendet das letzte Swing-Extrem, das von `SwingCandleType` mit `SwingBars`-Lookback berechnet wurde.
- **Gewinn mitnehmen**
  - *Feste Pips* – Verwendet `TakeProfitPips`.
  - *Risiko/Ertrag* – Verwendet die aktuelle Stoppdistanz multipliziert mit `TakeProfitRatio`.
- **Break-Even** – `MoveToBreakEven` definiert, wie viele profitable Pips erforderlich sind, bevor der Stop beim Einstiegspreis fixiert wird.
- **Trailing** – Wird von `TrailingStop`, `TrailingTrigger` und `TrailingStep` gesteuert, um Gewinne aufrechtzuerhalten, sobald sich der Markt positiv entwickelt.

## Parameter
| Gruppe | Name | Beschreibung |
| --- | --- | --- |
| Allgemein | `BuyMode` | Lange Einträge zulassen. |
| Allgemein | `SellMode` | Erlauben Sie kurze Einträge. |
| Allgemein | `CandleType` | Handelszeitrahmen (Standard 1 Stunde). |
| Zeitplan | `StartTime` / `EndTime` | Sitzungsfenster in Austauschzeit (00:00 → deaktiviert). |
| Filter | `UseMacdFilter` | Aktivieren Sie die MACD-Bestätigung. |
| Filter | `MacdFast`, `MacdSlow`, `MacdSignal` | MACD Perioden für schnelles EMA, langsames EMA und Signal EMA. |
| Risiko | `StopLossMode` | Stop-Loss-Berechnung: `FixedPips`, `AtrMultiplier`, `SwingHighLow`. |
| Risiko | `StopLossPips` | Entfernung in Pips, wenn der feste Modus ausgewählt ist. |
| Risiko | `AtrMultiplier`, `AtrPeriod`, `AtrCandleType` | ATR-basierte Stoppkonfiguration. |
| Risiko | `SwingBars`, `SwingCandleType` | Schwenk-Hoch-/Tief-Stopp-Konfiguration. |
| Risiko | `TakeProfitMode` | Zielmodus: `FixedPips` oder `RiskReward`. |
| Risiko | `TakeProfitPips`, `TakeProfitRatio` | Zielentfernungen. |
| Risiko | `CloseOnReverse` | Schließen Sie die aktive Position, wenn das entgegengesetzte Signal erscheint. |
| Bestellungen | `Volume` | Market-Order-Volumen (Lots/Kontrakte). |
| Risiko | `MoveToBreakEven` | Gewinnschwelle (in Pips), um den Stopp zum Einstieg zu bewegen. |
| Risiko | `TrailingStop`, `TrailingTrigger`, `TrailingStep` | Trailing-Stop-Konfiguration in Pips. |

## Nutzungshinweise
- Stellen Sie sicher, dass für das Instrument `PriceStep` definiert ist. andernfalls geht die Strategie von einer Pip-Größe von `0.0001` aus.
- Wenn ATR oder Swing Stops aktiviert sind, werden die entsprechenden Zusatzabonnements automatisch hinzugefügt. Stellen Sie sicher, dass der Datenfeed diese Zeitrahmen bereitstellt.
- Wenn Sie das Break-even- oder Trailing-Verhalten deaktivieren müssen, setzen Sie die entsprechenden Parameter auf `0`.
- Die Strategie ist bei Sitzungseröffnung standardmäßig neutral. Es werden nicht mehrere Positionen in die gleiche Richtung gestapelt; Wiedereinstiege erfolgen erst, nachdem der vorherige Handel geschlossen wurde.

## Einschränkungen im Vergleich zur MQL-Version
- Es werden nur Nettopositionen unterstützt (StockSharp-Einschränkung). Gleichzeitige Long- und Short-Trades im Hedge-Stil werden nicht reproduziert.
- Geldmanagement-Modi wie Kelly-Sizing oder teilweise Gewinnmitnahmen sind nicht Teil dieses Ports.
- Auf manuelle Bestätigung, Dashboard-Grafiken und Screenshot-Funktionen der Vorlage MQL wurde bewusst verzichtet.

## Checkliste für historische Tests
1. Konfigurieren Sie die gewünschten `CandleType`- und Hilfszeitrahmen.
2. Passen Sie die Parameter `Volume` und Stopp/Ziel an die ursprünglichen EA-Einstellungen an.
3. Aktivieren oder deaktivieren Sie die MACD-Bestätigung je nach Vorlagenverwendung.
4. Führen Sie eine Simulation durch und stellen Sie sicher, dass das Handelssitzungsfenster Ihren ursprünglichen Tests entspricht.
5. Überprüfen Sie die generierten Protokollmeldungen, um zu bestätigen, dass Stopp- und Zielereignisse wie erwartet eintreten.
