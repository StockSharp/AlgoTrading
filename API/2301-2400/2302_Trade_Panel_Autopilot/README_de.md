# Trade-Panel-Autopilot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie reproduziert die Kernlogik des ursprünglichen MQL4-Experten "trade panel with autopilot". Sie aggregiert die Preisrichtung über mehrere Zeitrahmen und öffnet oder schließt eine einzelne Position entsprechend der vorherrschenden Marktstimmung.

Die Strategie überwacht die letzten zwei Kerzen in acht verschiedenen Zeitrahmen (M1, M5, M15, M30, H1, H4, D1, W1). Für jeden Zeitrahmen vergleicht sie mehrere Preiskomponenten zwischen den beiden aktuellsten Kerzen:

- Open
- High
- Low
- (High + Low) / 2
- Close
- (High + Low + Close) / 3
- (High + Low + Close + Close) / 4

Jeder Vergleich trägt zu einem **Kauf**- oder **Verkaufs**-Score bei. Scores aus allen Zeitrahmen werden summiert und in Prozente umgerechnet. Wenn der Kauf- oder Verkaufsprozentsatz einen konfigurierten Schwellenwert überschreitet, eröffnet die Strategie eine Position. Die bestehende Position wird geschlossen, wenn der entgegengesetzte Prozentsatz unter den Schließungsschwellenwert fällt.

## Parameter

- `Autopilot` — aktiviert oder deaktiviert den automatischen Handel.
- `OpenThreshold` — Prozentniveau zum Öffnen einer neuen Position. Standard: 85.
- `CloseThreshold` — Prozentniveau zum Schließen einer bestehenden Position. Standard: 55.
- `LotFixed` — festes Ordervolumen wenn `UseFixedLot` aktiviert ist.
- `LotPercent` — Volumen als Prozentsatz des Portfoliowerts wenn `UseFixedLot` deaktiviert ist.
- `UseFixedLot` — wechselt zwischen festem und prozentualem Volumen.
- `UseStopLoss` — aktiviert den Positionsschutz wenn eingeschaltet.

## Handelslogik

1. Kerzen auf allen konfigurierten Zeitrahmen abonnieren.
2. Kauf-/Verkaufs-Scores für jede neue abgeschlossene Kerze berechnen.
3. Scores über alle Zeitrahmen summieren und Kauf-/Verkaufsprozentsätze berechnen.
4. Wenn `Autopilot` deaktiviert ist, verfolgt die Strategie nur die Scores.
5. Wenn keine Position offen ist und der Kaufprozentsatz `OpenThreshold` überschreitet, eine Long-Position eingehen. Wenn der Verkaufsprozentsatz den Schwellenwert überschreitet, eine Short-Position eingehen.
6. Wenn eine Long-Position besteht und der Kaufprozentsatz unter `CloseThreshold` fällt, die Position schließen. Die gleiche Logik gilt für Short-Positionen unter Verwendung des Verkaufsprozentsatzes.

## Hinweise

- Die Strategie hält zu einem Zeitpunkt höchstens eine offene Position.
- Optionales Stop-Loss-Management wird über `StartProtection()` aktiviert, wenn `UseStopLoss` wahr ist.
