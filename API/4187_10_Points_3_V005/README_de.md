# Zehn Punkte 3 v005 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters „10points 3 v005“. Es folgt der MACD-Steigung, um zu entscheiden, ob der aktuelle Durchschnittskorb long oder short sein soll, und eröffnet jedes Mal Martingal-Orders, wenn sich der Preis um eine konfigurierbare Distanz gegen die aktive Position bewegt. Die erweiterte Version „v005“ fügt eigenkapitalbasierte Schutzregeln, Tages- und Zeitfilter und die Option hinzu, entweder den langen oder den kurzen Zyklus zu deaktivieren.

## Handelslogik
- Lesen Sie die Richtung aus der Hauptzeile MACD ab. Wenn der Indikator steigt, wird der nächste Korb long sein, wenn er fällt, wird der Korb short sein. Eine Option erlaubt die Umkehrung der Interpretation.
- Eröffnen Sie sofort die erste Marktposition, sobald eine Richtung vorliegt. Nachfolgende Einträge werden hinzugefügt, wenn sich der Preis um `EntryDistancePips` gegenüber der Floating-Position bewegt.
- Bestellgrößen wachsen geometrisch. Der Multiplikator wird durch `MartingaleFactor` gesteuert (oder `HighTradeFactor`, wenn mehr als 12 Trades zulässig sind). Die Volumina richten sich nach der Instrumentenvolumenstufe und sind auf 100 Lots begrenzt.
- Jeder Eintrag aktualisiert die aggregierten Stop-Loss- und Take-Profit-Level. Die Anfangswerte werden um `InitialStopPips` und `TakeProfitPips` ausgeglichen, während die nachgestellte Logik aktiviert wird, nachdem die Position `EntryDistancePips + TrailingStopPips` dafür gewonnen hat.
- Wenn der Kontoschutz aktiviert ist, kann die Strategie das Ziel mit dem besten Eintrag (`ReboundLock`) ausrichten und die letzte Order schließen, sobald der variable Gewinn `SecureProfit` erreicht.
- Die Eigenkapitalschutzregeln schließen den gesamten Korb, wenn der variable Verlust `StopLossAmount` übersteigt, wenn das Eigenkapital über `ProfitTarget + ProfitBuffer` steigt oder wenn das Eigenkapital unter `StartProtectionLevel` fällt.
- Der Handel ist auf das Zeitfenster `OpenHour`/`CloseHour` beschränkt und freitags standardmäßig vollständig deaktiviert.

## Geldmanagement
Wenn `UseMoneyManagement` deaktiviert ist, verwendet die erste Bestellung das feste `LotSize`. Wenn das Flag aktiviert ist, wird das Basisvolumen aus dem aktuellen Portfoliowert und dem Parameter `RiskPercent` berechnet. Die Skalierung von Minikonten kann durch `IsStandardAccount` simuliert werden.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TakeProfitPips` | Abstand (in Pips) des Take-Profits, der auf jeden Eintrag angewendet wird. |
| `LotSize` | Basislosgröße, wenn die Geldverwaltung deaktiviert ist. |
| `InitialStopPips` | Anfängliche Stop-Loss-Distanz für jede Order. |
| `TrailingStopPips` | Trailing-Stop-Distanz, sobald die Triggerschwelle erreicht ist. |
| `MaxTrades` | Maximale Anzahl gleichzeitiger Martingaleinträge. |
| `EntryDistancePips` | Mindestens erforderliche Gegenbewegung zum Hinzufügen der nächsten Bestellung. |
| `SecureProfit` | Variabler Gewinn (in Währungseinheiten), der erforderlich ist, um den Ausstieg aus dem Kontoschutz auszulösen. |
| `UseAccountProtection` | Aktiviert die Secure-Profit- und Rebound-Lock-Logik. |
| `OrdersToProtect` | Anzahl der letzten Martingalschritte, die durch die Secure-Profit-Regel geschützt sind. |
| `ReverseSignals` | Kehrt die Interpretation der MACD-Steigung um. |
| `UseMoneyManagement` | Ermöglicht die ausgleichsbasierte Größenbestimmung. |
| `RiskPercent` | Von der Money-Management-Formel verwendeter Risikoprozentsatz. |
| `IsStandardAccount` | Verwendet die Standardlosskalierung anstelle der Miniskalierung. |
| `EurUsdPipValue`, `GbpUsdPipValue`, `UsdChfPipValue`, `UsdJpyPipValue`, `DefaultPipValue` | Pip-Werte, die zur Umrechnung variabler Gewinne in eine Währung verwendet werden. |
| `CandleType` | Kerzenzeitrahmen, der für die Signalgenerierung verwendet wird. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD-Konfiguration. |
| `EnableLong`, `EnableShort` | Aktivieren oder deaktivieren Sie den langen/kurzen Korb. |
| `OpenHour`, `CloseHour`, `MinuteToStop` | Konfiguration des Handelsfensters. |
| `StopLossProtection`, `StopLossAmount` | Aktienbasierter Stop-Loss-Schutz. |
| `ProfitTargetEnabled`, `ProfitTarget`, `ProfitBuffer` | Eigenkapitalbasierte Gewinnsperre. |
| `StartProtectionEnabled`, `StartProtectionLevel` | Equity-Bodenwächter. |
| `ReboundLock` | Richtet Ausgänge am besten Eingang aus, wenn der Schutz aktiv ist. |
| `MartingaleFactor`, `HighTradeFactor` | Martingale Multiplikatoren. |
| `CloseOnFriday` | Deaktiviert den Handel freitags. |

## Notizen
- Die Strategie verwendet das übergeordnete StockSharp API (`SubscribeCandles` + `BindEx`) und stellt keine rohen Indikatorpuffer bereit.
- Jeder Aktienwächter schließt den aktiven Warenkorb mithilfe von Marktaufträgen, um das ursprüngliche EA-Verhalten zu reproduzieren.
- Überprüfen Sie immer die Parameterwerte, die Pip-Größe und den Pip-Wert anhand Ihrer Broker-Spezifikationen, bevor Sie die Strategie in der Produktion verwenden.
