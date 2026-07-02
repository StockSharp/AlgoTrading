# Duale Stoploss-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert das Verhalten des MetaTrader-Experten **Dual StopLoss.mq4**. Es fungiert als Risikomanagementebene: Es überwacht die schützenden Stop-Loss-Aufträge, die mit offenen Positionen verbunden sind, und schließt diese Positionen einige Punkte, bevor der Stop ausgelöst wird. Der frühe Ausstieg soll einen negativen Slippage bei stark volatilen Bewegungen vermeiden und gleichzeitig die anfängliche Stop-Platzierung des Händlers respektieren.

## Wie es funktioniert

1. Die Strategie abonniert Level1-Daten, um den aktuell besten Geld-/Briefkurs und den vom Broker veröffentlichten `StopLevel`-Abstand (oder einen gleichwertigen Abstand) zu verfolgen.
2. Jedes Mal, wenn neue Preise eintreffen oder sich Aufträge/Geschäfte ändern, wird nach der nächstgelegenen aktiven Stop-Order gesucht, die zum verwalteten Wertpapier gehört.
3. Der Abstand zwischen dem Marktpreis und diesem Schutzstopp wird mit einem konfigurierbaren Schwellenwert verglichen:
   - Schwellenwert = `WhenToClosePoints × pointValue + stopLevelDistance`.
   - `pointValue` stimmt mit MetaTraders `Point` überein (0,0001 für die meisten FX-Paare, automatisch aus den Sicherheitseinstellungen erkannt).
   - `stopLevelDistance` stammt aus Level1-Feldern (`StopLevel`, `MinStopPrice`, `StopPrice` oder `StopDistance`), sofern verfügbar, andernfalls Null.
4. Wenn die verbleibende Distanz kleiner oder gleich dem Schwellenwert ist, wird die Position sofort mit einer Marktorder geschlossen.

Die Logik umfasst sowohl Long- als auch Short-Positionen. Bei Long-Positionen wird das beste Gebot mit dem Sell-Stop-Preis verglichen; Bei Short-Positionen wird der beste Ask-Preis mit dem Buy-Stop-Preis verglichen. Es werden nur Stop- und Stop-Limit-Orders im aktiven Zustand berücksichtigt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| **WhenToClosePoints** | Entfernung (in MetaTrader Punkten) von der Stoppebene, die den vorzeitigen Ausstieg auslösen soll. Standard: 10. Auf Null gesetzt, um sich nur auf die minimale Stop-Level-Distanz des Brokers zu verlassen. |

## Hinweise und Einschränkungen

- Die Strategie eröffnet **keine** Positionen allein; Es verwaltet nur Positionen, die bereits vorhanden sind und über schützende Stop-Orders verfügen.
- Stellen Sie sicher, dass der zugrunde liegende Connector/Broker Stop-Level-Werte über Level1-Daten bereitstellt, wenn Sie vom Broker vorgegebene Mindestabstände berücksichtigen möchten. Fehlen diese Informationen, funktioniert die Strategie trotzdem nur mit der konfigurierten Punktentfernung.
- Der `StartProtection()`-Anruf aktiviert die integrierten Sicherheitsvorrichtungen von StockSharp, sodass Notausgänge aktiv bleiben, sobald die Strategie gestartet wurde.
- Stopps werden aus der `Orders`-Sammlung der Strategie erkannt. Stellen Sie sicher, dass Schutzstopps über dieselbe Strategieinstanz registriert werden, damit sie in dieser Liste erscheinen.
- Wenn mehrere Stop-Orders für die gleiche Richtung vorhanden sind, wird diejenige verwendet, die dem Markt am nächsten liegt.

## Anwendungstipps

1. Hängen Sie die Strategie an ein Portfolio/Wertpapier an, bei dem Positionen manuell oder durch ein anderes System eröffnet werden, aber Schutzstopps im gleichen Strategiekontext platziert werden.
2. Konfigurieren Sie `WhenToClosePoints` entsprechend der Menge an Polsterung, die Sie vor dem Stopp benötigen. Dieser Wert wird genau wie in MetaTrader interpretiert (Punkte, keine Preiseinheiten).
3. Starten Sie die Strategie und überwachen Sie das Protokoll. Wenn sich der Marktpreis dem Stop nähert, erteilt die Strategie einen Marktauftrag, um die Position proaktiv zu schließen.
4. Kombinieren Sie dieses Modul mit anderen Einstiegs- oder Positionsgrößenstrategien, um einen vollständigen Handelsworkflow zu erstellen.
