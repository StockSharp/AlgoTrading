# BrakeoutTraderV1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

BrakeoutTraderV1 ist ein einfaches Ausbruchssystem, das auf einem statischen Preisniveau basiert. Die Strategie beobachtet die Schlusskurse abgeschlossener Kerzen und steigt ein, wenn der Markt durch das gewählte Ausbruchsniveau schließt. Wenn der Schluss über das Niveau steigt, wird eine Long-Position eröffnet (abhängig von Richtungsfiltern); wenn er darunter fällt, wird eine Short-Position eingegangen. Die Positionsgröße wird aus dem konfigurierten Risikoanteil und dem Abstand zum Stop-Loss berechnet, was eine automatische Skalierung mit dem Kontokapital ermöglicht.

## Handelslogik
- Verarbeite nur abgeschlossene Kerzen des ausgewählten `CandleType`. Unvollständige Kerzen werden ignoriert.
- Halte den zuletzt geschlossenen Preis, um Ausbrüche des benutzerdefinierten `BreakoutLevel` zu erkennen.
- **Long-Einstieg**: Die neueste Kerze schließt über `BreakoutLevel`, während der vorherige Schluss auf oder unter dem Niveau lag, und `EnableLong` ist wahr. Jede offene Short-Position wird vor der neuen Order glattgestellt.
- **Short-Einstieg**: Die neueste Kerze schließt unter `BreakoutLevel`, während der vorherige Schluss auf oder über dem Niveau lag, und `EnableShort` ist wahr. Jede Long-Position wird zuerst geschlossen.
- Orders werden zu Marktpreisen gesendet. Die Menge wird so berechnet, dass der Verlust zwischen Einstiegspreis und Stop-Loss-Abstand dem `RiskPercent` des aktuellen Kontokapitals entspricht. Wenn die risikobasierte Größe nicht bestimmt werden kann, fällt die Strategie auf den Basis-`Volume`-Wert zurück.
- Nach jedem Einstieg speichert die Strategie statische Gewinnmitnahme- und Stop-Loss-Niveaus in Pips (`StopLossPoints` und `TakeProfitPoints`). Wenn der Preis eines der Niveaus erreicht, wird die offene Position zu Marktpreisen geschlossen und die gecachten Niveaus werden gelöscht.
- Es gibt niemals mehrere gleichzeitige offene Trades in dieselbe Richtung, da die Nettoposition explizit verwaltet wird.

## Positionsverwaltung
- Ein Schutz-Stop wird unterhalb des Einstiegs für Long-Trades und oberhalb für Shorts gesetzt. Der Abstand beträgt `StopLossPoints * Pip`, wobei Pip aus `Security.PriceStep` und seiner Genauigkeit abgeleitet wird (3 oder 5 Dezimalstellen implizieren eine zehnfache Anpassung, wie in der originalen MQL-Implementierung).
- Ein Gewinnziel wird symmetrisch mit `TakeProfitPoints` gesetzt.
- Wenn sowohl Stop als auch Ziel in derselben Kerze ausgelöst würden, wird der Stop zuerst bewertet, was konservative serverseitige Ausführung widerspiegelt.
- Gegensätzliche Signale schließen immer jede aktive Position, bevor die neue aufgebaut wird, um gehämmerte Exposition zu verhindern.
- Der Helper setzt gecachte Niveaus automatisch zurück, wenn die Position auf null zurückkehrt.

## Parameter
- `BreakoutLevel` – Statisches Preisniveau, das auf Ausbrüche überwacht wird.
- `EnableLong` / `EnableShort` – Richtungsfilter, die das Öffnen von Long- oder Short-Positionen erlauben.
- `StopLossPoints` – Stop-Loss-Abstand in Pips (Vielfache der abgeleiteten Pip-Größe).
- `TakeProfitPoints` – Take-Profit-Abstand in Pips.
- `RiskPercent` – Prozentsatz des Kontokapitals, das pro Trade riskiert wird. Wird verwendet, um das Ordervolumen aus dem Stop-Loss-Abstand zu bestimmen.
- `CandleType` – Kerzendatenserie für die Signalgenerierung (Standard: 15-Minuten-Kerzen).
- `Volume` – Basisordergröße, die verwendet wird, wenn die risikobasierte Berechnung nicht verfügbar ist.

## Details
- **Einstiegskriterien**: Schluss kreuzt über/unter `BreakoutLevel` bei der letzten abgeschlossenen Kerze.
- **Long/Short**: Handelt beide Richtungen, gesteuert durch die `EnableLong`- und `EnableShort`-Flags.
- **Ausstiegskriterien**: Statische Stop-Loss- und Take-Profit-Niveaus sowie Glattstellung bei entgegengesetzten Ausbruchssignalen.
- **Stops**: Fest-Abstands-Stop-Loss in Pips gemessen.
- **Standardwerte**: `BreakoutLevel = 0`, `StopLossPoints = 140`, `TakeProfitPoints = 180`, `RiskPercent = 10`, `CandleType = 15 Minuten`, `EnableLong = EnableShort = true`.
- **Filter**: Keine über die Richtungsschalter hinaus; keine Trend- oder Volatilitätsfilter werden angewendet.

## Verwendungshinweise
- Das Instrument sollte die vom Original-EA verwendete Pip-Berechnung unterstützen. Bei Symbolen mit 3 oder 5 Dezimalstellen wird der Pip automatisch um zehn skaliert.
- Sicherstellen, dass das verbundene Portfolio `CurrentValue` bereitstellt, damit die risikobasierte Größenberechnung korrekt funktioniert. Wenn das Kapital nicht verfügbar ist, fallen Trades auf das konfigurierte `Volume` zurück.
- Da Orders zu Marktpreisen ausgeführt werden, können die tatsächlichen Füllungen vom Kerzenschluss abweichen. Stop- und Take-Abstände bei Bedarf anpassen, um Slippage zu berücksichtigen.
