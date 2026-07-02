# AK-47 Scalper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Umsetzung des MetaTrader 5 Expert Advisors **„AK-47 Scalper EA“ (Build 44883)**. Es stellt das ursprüngliche Verhalten innerhalb des übergeordneten Strategie-Frameworks StockSharp wieder her.

Der Algorithmus hält eine einzelne *Verkaufsstopp*-Order während der zulässigen Handelszeiten aktiv. Sobald die Order ausgelöst wird, fügt die Strategie sofort schützende Stop-Loss- und Take-Profit-Orders hinzu. Sowohl der Pending-Order-Preis als auch der Schutzstopp werden je nach Marktbewegung dynamisch verschärft.

## Kernlogik

1. Berechnen Sie die Pip-Größe anhand der Tick-Größe des Instruments (5-stellige Symbole verwenden 0,1-Pip-Schritte, genau wie in MetaTrader).
2. Bestimmen Sie das Handelsfenster. Bei aktiviertem Zeitfilter sind Eingaben nur zwischen den konfigurierten Start- und Endzeiten (einschließlich Start, exklusive Ende) zulässig. Nachtsitzungen werden durch einen Abschluss um Mitternacht unterstützt.
3. Stellen Sie sicher, dass der aktuelle Spread in Punkten das konfigurierte Limit nicht überschreitet, bevor Sie neue Bestellungen aufgeben.
4. Größe der Position:
   - Verwenden Sie entweder die feste Menge (Parameter `Base Lot`) oder
   - Konvertieren Sie den konfigurierten `Risk Percent` des Portfoliowerts in Lots (nachahmen Sie die MT5-Formel) und richten Sie ihn an den Beschränkungen des Börsenvolumens aus.
5. Platzieren Sie eine Verkaufsstopp-Order `SL/2` Pips unter dem Gebot. Der Schutzstopp liegt voraussichtlich `SL/2` Pips über dem Briefkurs und der Take-Profit liegt `TP` Pips unter dem Einstiegspunkt.
6. Während die Order aussteht, wird sie von der Strategie kontinuierlich neu registriert, um die SL/2-Pip-Lücke zum Gebot beizubehalten und die geplanten Schutzpreise zu aktualisieren.
7. Nach der Ausführung:
   - Registrieren Sie eine Buy-Stop-Stop-Loss-Order und eine Buy-Limit-Take-Profit-Order zu den geplanten Preisen.
   - Bei jedem Kerzenschluss folgt die Strategie dem Stop, indem sie ihn genau `SL` Pips über dem aktuellen Gebot hält (ohne ihn zu lockern).
   - Der Take-Profit-Preis bleibt fest, sobald er festgelegt wurde.
8. Wenn die Position flach ist, werden alle Schutzanweisungen aufgehoben und ein neuer Zyklus kann beginnen.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| **Risikoprozentsatz verwenden** | Wechseln Sie zwischen festen Losgrößen und eigenkapitalbasierter Größenbestimmung. |
| **Risikoprozentsatz** | Prozentsatz, der bei der Berechnung des Handelsvolumens auf den Portfoliowert angewendet wird. |
| **Grundgrundstück** | Die Losgröße und der Rundungsschritt für die Positionsgröße wurden korrigiert. |
| **Stop-Loss (Pips)** | Abstand zwischen dem Einstiegspreis und dem Schutzstopp. Der Pending-Order-Offset verwendet die Hälfte dieser Distanz. |
| **Gewinnmitnahme (Pips)** | Gewinnzielentfernung. Auf Null setzen, um das Ziel zu deaktivieren. |
| **Maximaler Spread (Punkte)** | Maximal zulässiger Spread (in MetaTrader Punkten) für den Markteintritt. |
| **Zeitfilter verwenden** | Aktivieren oder deaktivieren Sie die Handelsfensterbeschränkung. |
| **Startstunde/Minute** | Beginn des Handelsfensters. |
| **Stunde/Minute beenden** | Ende des Handelsfensters. |
| **Kerzentyp** | Candle-Abonnement, das für Zeit- und Preisaktualisierungen verwendet wird. |

## Notizen

- Die Strategie verwendet nur kurze Einträge, genau wie das Original EA.
- Das Trailing wird bei Kerzenschluss durchgeführt, um mit dem StockSharp-High-Level API synchron zu bleiben.
- Schutzaufträge werden über `ReRegisterOrder`-Aufrufe ersetzt, daher muss die Börse oder der Simulator die Auftragsersetzung unterstützen.
- Die ursprünglichen grafischen Kommentare von MetaTrader werden nicht reproduziert, da die Strategien von StockSharp auf Protokollierung statt auf Terminalkommentaren basieren.
