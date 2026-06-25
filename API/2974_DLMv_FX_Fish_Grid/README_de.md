# DLMv FX Fish Grid Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **DLMv FX Fish Grid Strategie** repliziert das Verhalten des ursprünglichen MetaTrader Expert Advisors, der um den "FX Fish 2MA" Oszillator aufgebaut wurde. Die Strategie wertet die Fisher-Transformation des Preises aus, glättet ihn mit einem gleitenden Durchschnitt und öffnet Positionen, wenn der Oszillator seine geglättete Basislinie auf der richtigen Seite von null kreuzt. Das Positionsmanagement ahmt das gitterartige Verhalten des Quell-EA nach: Zusätzliche Einstiege sind durch eine konfigurierbare Distanz voneinander getrennt, ausstehende Limit-Orders können gestaffelt werden, und Schutzautomatisierung handhabt Risikokontrollen.

## Handelslogik

1. **Indikatorberechnung**
   - Höchste und niedrigste Preise über `CalculatePeriod` Kerzen definieren den gleitenden Bereich.
   - Eine Fisher-Transformation wird auf den ausgewählten Preis (`AppliedPrice`) angewendet, mit demselben 0.67 Glättungsfaktor wie der MT5-Indikator.
   - Ein einfacher gleitender Durchschnitt (`MaPeriod`) des Fisher-Werts liefert die Signal-Basislinie.
2. **Signalerzeugung**
   - **Long-Signal**: Aktuelle und vorherige Fisher-Werte liegen unter null, während der Oszillator **über** seinen gleitenden Durchschnitt kreuzt (vorheriger Wert unter Durchschnitt, aktueller Wert darüber).
   - **Short-Signal**: Aktuelle und vorherige Fisher-Werte liegen über null, während der Oszillator **unter** den gleitenden Durchschnitt kreuzt (vorheriger Wert über Durchschnitt, aktueller Wert darunter).
   - Signale können durch Aktivierung von `ReverseSignals` invertiert werden.
3. **Orderausführung**
   - Wenn ein Kauf- (oder Verkaufs-)Signal erscheint, kann die Strategie optional bestehende entgegengesetzte Exposition schließen (`CloseOpposite`).
   - Zusätzliche Einstiege sind erlaubt, bis die Gesamtzahl `MaxTrades` erreicht. Jeder neue Einstieg muss den minimalen Abstand von `DistancePips` vom letzten gefüllten Trade einhalten.
   - Optionale Limit-Orders (`SetLimitOrders`) platzieren ruhende Gebote/Angebote beim konfigurierten Abstand und replizieren das gestaffelte Gitter des ursprünglichen EA.
4. **Risikomanagement**
   - Feste Stop-Loss-, Take-Profit- und Trailing-Stop-Werte werden über `StartProtection` angewendet, alle in Pips definiert.
   - `TimeLiveSeconds` schließt alle Exposition, wenn ein Trade länger als die erlaubte Lebensdauer offen war.
   - Das Trading kann an Freitagen deaktiviert werden (`TradeOnFriday = false`). Wenn deaktiviert, schließt die Strategie Positionen und storniert ausstehende Orders, sobald eine Freitag-Kerze eintrifft.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `OrderVolume` | Ordergröße für jeden Einstieg (Lots). |
| `StopLossPips` | Abstand des schützenden Stop-Loss vom Einstieg. Auf 0 setzen zum Deaktivieren. |
| `TakeProfitPips` | Abstand des Take-Profit-Niveaus. Auf 0 setzen zum Deaktivieren. |
| `TrailingStopPips` | Trailing-Stop-Abstand (0 deaktiviert Trailing). |
| `TrailingStepPips` | Schritt, um den der Trailing Stop gestrafft wird. |
| `MaxTrades` | Maximale Anzahl gleichzeitiger Trades pro Richtung. `0` entfernt das Limit. |
| `DistancePips` | Mindestabstand zwischen aufeinanderfolgenden Einstiegen und für die optionalen Gitter-Orders. |
| `TradeOnFriday` | Wenn `false`, stoppt die Strategie den Handel an Freitagen und liquidiert die Exposition. |
| `TimeLiveSeconds` | Maximale Zeit (Sekunden), die Positionen offen bleiben dürfen, bevor sie zwangsgeschlossen werden. |
| `ReverseSignals` | Long/Short-Bedingungen invertieren. |
| `SetLimitOrders` | Zusätzliche ruhende Limit-Orders bei `DistancePips` aktivieren. |
| `CloseOpposite` | Entgegengesetzte Exposition schließen, bevor ein neuer Trade eröffnet wird. |
| `CalculatePeriod` | Lookback für den Bereich der Fisher-Transformation. |
| `MaPeriod` | Periode des auf den Fisher-Wert angewendeten gleitenden Durchschnitts. |
| `AppliedPrice` | Preisquelle für die Fisher-Transformation (close, open, high, low, median, typical, weighted). |
| `CandleType` | Datentyp/Zeitrahmen der von der Strategie verarbeiteten Kerzen. |

## Hinweise

- Die Stop-Loss-, Take-Profit- und Trailing-Stop-Abstände werden von Pips in absolute Preisoffsets umgerechnet mit `Security.PriceStep * 10`, was der Fünf-Stellen-Pip-Logik der MQL-Version entspricht.
- Limit-Orders werden automatisch storniert, wenn Signale wechseln, der Handel pausiert wird oder Zeit-/Freitagsschutzmaßnahmen ausgelöst werden.
- Die Fisher-Transformation vermeidet wiederholte Wert-Lookups und speichert stattdessen die vorherigen Oszillator- und Basislinienwerte für eine präzise Kreuzerkennung.
