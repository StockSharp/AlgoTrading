# Take-Profit-Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den „Take-Profit“-Experten von MetaTrader, indem sie nach vier aufeinanderfolgenden Kerzen mit streng monotonen Höchst- und Eröffnungswerten sucht. Wenn die aktuelle Kerze mit steigenden Höchstständen endet und sich öffnet, behandelt der Algorithmus die Sequenz als bullisches Momentum und übermittelt einen Marktkauf. Ein gespiegelter Zustand mit fallenden Höchstständen und Eröffnungen führt zu einem Marktverkauf. Aufträge werden mit einem Gewinnziel auf Kontoebene, einem Trailing Stop, der das Risiko teilweise schließen kann, und einem optionalen festen Stop-Loss verwaltet, der in Preisschritten definiert ist.

Die Standardkonfiguration handelt mit Ein-Minuten-Kerzen. Die Strategie kann für verschiedene Instrumente angepasst werden, indem der Kerzentyp, die Verschiebungsindizes, die steuern, welche Kerzen verglichen werden, die Trailing-Distanz, die Stop-Loss-Distanz, das Gewinnziel und der Positionsgrößenmodus angepasst werden. Es unterstützt entweder eine feste Losgröße oder ein dynamisches Volumen, das aus dem Portfolio-Eigenkapital und einem benutzerdefinierten Risikoprozentsatz berechnet wird. Wenn der Trailing Stop vorrückt, kann der Algorithmus optional die Hälfte der verbleibenden Position schließen, um Gewinne zu sichern und gleichzeitig einen Läufer aktiv zu halten.

Bei Erreichen des konfigurierten Gewinnziels, gemessen am Portfolioeigenkapital, wird die aktuelle Position sofort liquidiert und alle funktionierenden Aufträge storniert. Dies spiegelt den ursprünglichen MQL-Experten wider, der alle Geschäfte schloss, wenn das Kontoguthaben den Saldo plus den gewünschten Gewinn überstieg. Der Risikomanagementzweig validiert den konfigurierten Risikoprozentsatz und stellt sicher, dass das angeforderte Volumen den Sicherheitsvolumenschritt respektiert.

## Einzelheiten

- **Eingabelogik**:
  - **Long**: Die vier überwachten Kerzen zeigen streng steigende Höchstwerte und streng steigende Eröffnungen.
  - **Short**: Die vier überwachten Kerzen zeigen streng fallende Höchstwerte und streng fallende Eröffnungen.
- **Positionsmanagement**:
  - Optionaler Stop-Loss beim Einstiegspreis minus/plus der konfigurierten Anzahl von Preisschritten.
  - Der Trailing-Stop folgt dem Schlusskurs, sobald er sich um mehr als die Trailing-Distanz vom Einstiegspunkt entfernt.
  - Ein teilweiser Ausstieg (50 % des verbleibenden Volumens) wird bei jeder Bewegung des Trailing Stops ausgeführt, abhängig von der Sicherheitsvolumenstufe und der minimal handelbaren Menge.
- **Kontoziel**: Schließt alle Engagements und storniert aktive Bestellungen, wenn `portfolio equity ≥ initial equity + ProfitTarget`.
- **Risikomanagement**:
  - Der feste Losmodus verwendet den konfigurierten Parameter `Lots` (oder `Volume` aus der Strategiebasis, falls angegeben).
  - Im Risikoprozentmodus wird die Order auf `equity * RiskPercent / max(stopDistance, price)` bemessen und das Ergebnis durch den Volumenschritt normalisiert.
- **Standardparameter**:
  - `Shift1` = 0, `Shift2` = 1, `Shift3` = 2, `Shift4` = 3.
  - `TrailingStopPoints` = 1, `StopLossPoints` = 0, `ProfitTarget` = 1 (Kontowährungseinheiten).
  - `Lots` = 1, `RiskPercent` = 1, `MaxOrders` = 1.
  - `CandleType` = 1-Minuten-Zeitrahmen.
- **Beste Märkte**: Trend-Futures, FX-Majors und liquide Kryptopaare, bei denen die kurzfristige Dynamik über mehrere Kerzen hinweg anhält.
- **Stärken**: schnelle Momentum-Erkennung, konfigurierbares Aktienziel, teilweises Scale-out und einfache Risikokontrolle.
- **Schwächen**: empfindlich gegenüber verrauschten Bereichen, hängt von der richtigen Schrittgröße ab und geht vom Netting-Modus (einzelne aggregierte Position) aus.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `Shift1` – `Shift4` | Indizes der für die Ausbruchssequenz verglichenen Kerzen. |
| `TrailingStopPoints` | Nachlaufdistanz in Preisschritten. |
| `StopLossPoints` | Anfängliche Stoppdistanz in Preisschritten; Null deaktiviert den Stop-Loss. |
| `ProfitTarget` | Das Gewinnziel wird auf das Portfolio-Eigenkapital angewendet, bevor alle Geschäfte geschlossen werden. |
| `Lots` | Festes Handelsvolumen bei deaktiviertem Risikomanagement. |
| `RiskManagement` | Ermöglicht die risikobasierte Größenbestimmung mit `RiskPercent`. |
| `RiskPercent` | Prozentsatz des Portfolio-Eigenkapitals, das bei jedem Trade riskiert wird, wenn das Risikomanagement aktiv ist. |
| `PartialClose` | Wenn aktiviert, wird die Hälfte der Position geschlossen, wenn sich der Trailing Stop bewegt. |
| `MaxOrders` | Maximale Anzahl gleichzeitig zulässiger Basiseinheiten (Nettopositionslimit). |
| `CandleType` | Zeitrahmen, der für die Signalgenerierung verwendet wird. |

## Nutzungstipps

1. Passen Sie die `Shift`-Parameter an die Volatilität des Instruments an. Größere Verschiebungen analysieren längere Impulssequenzen.
2. Legen Sie `TrailingStopPoints` relativ zum Wertpapierpreisschritt fest; Zu kleine Werte können zu schnellen Teilausstiegen führen.
3. Verwenden Sie eine risikoprozentuale Größenbestimmung mit einem expliziten `StopLossPoints`, damit die Positionsgröße das tatsächliche monetäre Risiko pro Trade widerspiegelt.
4. Beobachten Sie die Aktienkurve: Sobald das globale Ziel erreicht ist, stoppt die Strategie den Handel, bis sie neu gestartet wird, und ahmt so den ursprünglichen EA nach.
