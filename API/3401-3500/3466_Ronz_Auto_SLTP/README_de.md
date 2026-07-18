# Ronz Auto SLTP-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Ronz Auto SLTP-Strategie** ist eine direkte C#-Portierung des MetaTrader 5-Dienstprogramms *Ronz Auto SLTP*. Es fungiert als Handelsmanager, der automatisch schützende Stop-Loss- und Take-Profit-Levels festlegt, Gewinnsperren anwendet und Trailing-Regeln für jede offene Position aktiviert. Die Konvertierung basiert auf dem übergeordneten StockSharp API und unterstützt sowohl die kontoweite Überwachung als auch die Bereitstellung mit einem einzelnen Symbol.

Hauptfunktionen:

- Wenden Sie abhängig vom Flag `UseServerStops` serverseitigen oder virtuellen (clientseitigen) Schutz an.
- Legen Sie anfängliche Stop-Loss- und Take-Profit-Abstände mithilfe von Pip-Messungen im MetaTrader-Stil fest.
- Sichern Sie sich einen festen Gewinnbetrag, nachdem der Handel einen konfigurierbaren Schwellenwert erreicht.
- Führen Sie drei Trailing-Stop-Varianten (klassisch, Schrittdistanz, Schritt-für-Schritt) aus, die den ursprünglichen Ratgeber widerspiegeln.
- Überwachen Sie alle Wertpapiere im angeschlossenen Portfolio oder beschränken Sie die Verwaltung nur auf das Strategiepapier.
- Geben Sie optionale Protokollbenachrichtigungen aus, wenn ein virtueller Stop oder Take-Profit eine Position schließt.

## Parameter

| Parameter | Standard | Beschreibung |
| --- | --- | --- |
| `ManageAllSecurities` | `true` | Überwachen Sie jede offene Position im Portfolio. Deaktivieren Sie diese Option, um nur die Strategiesicherheit zu verwalten. |
| `TakeProfitPips` | `550` | Distanz in MetaTrader Pips, addiert zum Einstiegspreis für das Take-Profit-Ziel (einschließlich minimaler Stop-Distanz des Brokers). |
| `StopLossPips` | `350` | Distanz in MetaTrader Pips, abgezogen vom Einstiegspreis für das Stop-Loss-Level (einschließlich der minimalen Stop-Distanz des Brokers). |
| `UseServerStops` | `true` | Wenn aktiviert, senden Sie Stop- und Limit-Orders an den Broker. Wenn deaktiviert, werden Positionen praktisch geschlossen, sobald Schwellenwerte erreicht werden. |
| `EnableLockProfit` | `true` | Aktivieren Sie die Profit-Lock-Logik, die den Stop nach Erreichen eines Schwellenwerts über/unter den Einstiegspreis verschiebt. |
| `LockProfitAfterPips` | `100` | Gewinn (in Pips), der erzielt werden muss, bevor die Sperrlogik aktiv wird. Auf Null setzen, um die Sperrphase zu überspringen und sofort nachzufahren. |
| `ProfitLockPips` | `60` | Der Gewinn bleibt erhalten, sobald die Sperre aktiviert wird. Der Stop wird auf den Einstiegspreis plus/minus dieser Distanz verschoben. |
| `TrailingStopMode` | `Classic` | Nachgestellter Algorithmus, der nach dem Sperrschwellenwert verwendet wird. Optionen: `None`, `Classic`, `StepDistance`, `StepByStep`. |
| `TrailingStopPips` | `50` | Nachlaufdistanz in Pips. Fungiert als Hauptpuffer sowohl für den klassischen als auch für den schrittweisen Trailing-Modus. |
| `TrailingStepPips` | `10` | Inkrement, das von schrittbasierten Trailing-Modi verwendet wird. Wird von der klassischen Trailing-Variante ignoriert. |
| `EnableAlerts` | `false` | Bei „true“ werden Protokollmeldungen geschrieben, wenn ein virtueller Stop oder Take-Profit eine Order schließt. |

## Verhaltensdetails

1. **Erstschutz**
   - Wenn eine neue Position erkannt wird, berechnet die Strategie Stop-Loss- und Take-Profit-Ziele im Verhältnis zum Einstiegspreis.
   - Vom Broker definierte minimale Stoppabstände werden berücksichtigt, indem Stopp-/Freeze-Level-Felder aus Level1-Updates gelesen und die angeforderten Abstände bei Bedarf erweitert werden.

2. **Gewinnsperre**
   - Sobald der aktuelle Gewinn `LockProfitAfterPips` übersteigt, wird der Stop angehoben (oder bei Shorts gesenkt), um einen Gewinn im Wert von `ProfitLockPips` zu sichern.
   - Wenn das Sperren deaktiviert ist, überspringt die Strategie diese Phase und wartet auf die nachfolgenden Bedingungen.

3. **Trailing Stops**
   - `Classic`: hält einen festen Abstand (`TrailingStopPips`) zum aktuellen Preis.
   - `StepDistance`: Reduziert den Abstand um `TrailingStepPips`, sobald sich der Preis günstig genug bewegt hat, was weitgehend der MetaTrader-Implementierung „Schritt halten Abstand“ entspricht.
   - `StepByStep`: verschiebt den Stopp in diskreten `TrailingStepPips`-Schritten nach vorne, sobald der Preis um die konfigurierte Nachlaufdistanz vorgerückt ist.
   - Das Trailing beginnt sofort, wenn `LockProfitAfterPips` Null ist. Andernfalls wird es aktiviert, sobald der Gewinn `LockProfitAfterPips + TrailingStopPips` übersteigt.

4. **Virtueller Modus**
   - Wenn `UseServerStops` falsch ist, registriert die Strategie keine Stop-/Limit-Orders. Stattdessen wird die offene Position über Marktaufträge geschlossen, sobald der berechnete Stop-Loss oder Take-Profit überschritten wird.
   - Es können Warnungen aktiviert werden, um diese virtuellen Schließungen im Protokoll zu dokumentieren.

5. **Multi-Security-Unterstützung**
   - Mit `ManageAllSecurities = true` abonniert die Strategie Level1-Daten für jedes Wertpapier, das eine offene Position im ausgewählten Portfolio hat.
   - Jedes Wertpapier behält seinen eigenen Stop-, Take-Profit- und Trailing-Status bei, sodass Long- und Short-Trades unabhängig voneinander überwacht werden.

## Nutzungstipps

- Hängen Sie die Strategie an ein Portfolio an und weisen Sie optional eine Standardsicherheit zu, wenn nur ein Instrument überwacht werden muss.
- Stellen Sie sicher, dass für jedes verwaltete Symbol Level-1-Daten (bester Geld-/Briefkurs) verfügbar sind, damit die Pip-Berechnungen korrekt bleiben.
- Überprüfen Sie die Stop-Level-Beschränkungen des Brokers: Die Strategie erweitert bereits die erforderlichen Abstände, extrem enge Konfigurationen können jedoch weiterhin vom Handelsplatz abgelehnt werden.
- Der virtuelle Modus ist bei Brokern nützlich, die keine Schutzaufträge unterstützen, oder bei Backtesting-Szenarien.

## Unterschiede zum Original-Experten

- StockSharp aggregiert Positionen nach Wertpapieren, während der Absicherungsmodus MetaTrader einzelne Tickets verfolgt. Der Hafen verwaltet somit die Nettoposition pro Instrument.
- Auf die Test-Order-Funktionalität des MQ5-Skripts (Eröffnung von Dummy-Trades im Tester) wurde bewusst verzichtet.
- Warnungen werden über das Protokollierungssubsystem StockSharp und nicht über Popups auf dem Bildschirm übermittelt.
