# Backbone-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert das Kernverhalten des ursprünglichen **Backbone** MQL5 Expert Advisors mit der StockSharp High-Level-API. Sie wechselt zwischen bullischen und bärischen Handelszyklen, skaliert Positionen gemäß einem Risikoanteil und schützt offene Trades mit festen Zielen zusammen mit einem Trailing-Stop.

## Kernidee

1. **Erkennung der Anfangsrichtung** – die Strategie verfolgt das höchste Hoch und das niedrigste Tief nach dem Start. Eine Bewegung, die größer als der Trailing-Stop-Abstand von einem der Extreme ist, bestimmt, welche Seite zuerst handelt.
2. **Direktionale Zyklen** – nachdem ein Zyklus beginnt, handelt der Algorithmus nur in dieser Richtung, bis alle Positionen geschlossen sind. Wenn die letzte Position schließt, dreht er sofort um und bereitet sich auf den entgegengesetzten Zyklus vor.
3. **Risikobasierte Skalierung** – jeder zusätzliche Einstieg verwendet ein dynamisches Volumen, das aus dem Kontokapital, der `MaxRisk`-Fraktion, dem konfigurierten Limit `MaxTrades` und dem Stop-Loss-Abstand abgeleitet wird. Dies imitiert die Losgrößenfunktion des ursprünglichen EA.
4. **Schutzausstiege** – jeder Einstieg berechnet ein Stop-Loss- und Take-Profit-Niveau um den volumengewichteten Durchschnittspreis des aktuellen Zyklus neu. Ein Trailing-Stop strafft den Schutzstop, wenn der nicht realisierte Gewinn den konfigurierten Trailing-Abstand überschreitet.

## Parameter

| Parameter | Standardwerte | Beschreibung |
|-----------|---------|-------------|
| `MaxRisk` | 0.5 | Anteil des Kontokapitals, der für alle Positionen in der aktuellen Richtung verfügbar ist. |
| `MaxTrades` | 10 | Maximale Anzahl aufeinanderfolgender Einträge pro Direktionalzyklus. |
| `TakeProfitPips` | 170 | Abstand (in Pips) zwischen dem Einstiegsdurchschnitt und dem Take-Profit-Ziel. |
| `StopLossPips` | 40 | Abstand (in Pips) zwischen dem Einstiegsdurchschnitt und dem Schutzstop. |
| `TrailingStopPips` | 300 | Abstand (in Pips), der sowohl zur Bestimmung der Anfangsrichtung als auch zum Trailing von Gewinnen verwendet wird. |
| `CandleType` | 5-Minuten-Zeitrahmen | Kerzentyp für die Signalauswertung. |

> **Pip-Definition** – die Strategie passt die Pip-Größe automatisch basierend auf dem Instrument `PriceStep` an. Mit 3 oder 5 Dezimalstellen notierte Symbole verwenden einen 10×-Multiplikator, der das ursprüngliche MetaTrader-Pip-Handling repliziert.

## Handelslogik

1. Auf eine abgeschlossene Kerze warten. Verarbeitung überspringen, während die Strategie sich aufwärmt oder der Handel deaktiviert ist.
2. Die Extrempreise aktualisieren, solange noch keine Richtung gewählt wurde. Wenn das Hoch nach oben bricht (um mehr als `TrailingStopPips`) wird der erste Zyklus Short sein; wenn das Tief nach unten bricht, wird der erste Zyklus Long sein.
3. Während der Zyklus Long ist:
   - Einen neuen Long-Einstieg hinzufügen, wenn (a) der vorherige Zyklus Short war und keine Long-Positionen offen sind, oder (b) der vorherige Zyklus auch Long war und die Anzahl offener Longs unter `MaxTrades` liegt.
   - Den gesamten Long-Zyklus verlassen, wenn der Take-Profit oder Stop-Loss erreicht wird, oder wenn der Trailing-Stop das Schutzniveau über den aktuellen Stop anhebt.
4. Während der Zyklus Short ist, gelten dieselben Regeln mit umgekehrten Bedingungen.
5. Nachdem ein Zyklus schließt, seine Zähler zurücksetzen und auf das entgegengesetzte Setup warten.

## Positionsgrößenbestimmung

Die Positionsgröße für jeden neuen Einstieg wird berechnet als:

```
qty = equity * fraction / (pipSize * stopLoss)
wobei fraction = 1 / (MaxTrades / MaxRisk - openTrades)
```

Die Menge wird dann am Instrumentvolumenschritt ausgerichtet und innerhalb der Mindest-/Höchstvolumengrenzen begrenzt. Wenn die erforderliche Größe unter das erlaubte Minimum fällt, wird das Minimum verwendet. Wenn Kapitalinformationen nicht verfügbar sind, fungiert das Standard-Strategievolumen als Fallback.

## Ausgangsmanagement

- **Stop-Loss / Take-Profit** – wird bei jeder neuen Orderhinzufügung neu berechnet, damit alle Trades im aktuellen Zyklus dieselben kombinierten Niveaus basierend auf dem durchschnittlichen Einstiegspreis teilen.
- **Trailing-Stop** – bei einem Long-Zyklus bewegt sich der Stop auf `Close - TrailingStopPips * pipSize`, sobald der nicht realisierte Gewinn diesen Schwellenwert überschreitet. Das Short-Seiten-Trailing wird symmetrisch behandelt.

## Hinweise und Einschränkungen

- StockSharp führt Trades in einer Netting-Umgebung aus, daher verwaltet jeder direktionale Zyklus die kombinierte Position anstelle einzelner Tickets. Die abwechselnde Logik und die Risikoformel reproduzieren das ursprüngliche Verhalten, während sie zum API-Modell passen.
- Die Strategie stützt sich auf abgeschlossene Kerzen. Intrabar-Bewegungen, die kleiner als der Kerzenbereich sind, werden nicht ausgewertet.
- Sicherstellen, dass der ausgewählte Kerzentyp und das Wertpapier genügend Daten produzieren, um die anfänglichen Extreme zu bilden, bevor Trades erwartet werden.
