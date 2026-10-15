# Avalanche AV Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Avalanche AV ist eine randomisierte Martingal-Strategie, die mit gleicher Wahrscheinlichkeit zwischen Long- und Short-Einstiegen abwechselt. Trades werden nur nach einer konfigurierbaren Anzahl abgeschlossener Kerzen geöffnet, und jede Position erbt feste Stop-Loss- und Take-Profit-Levels in Pips. Wenn ein Trade mit Verlust schließt, wird die Positionsgröße mit dem Martingal-Koeffizienten multipliziert, um die Erholung anzustreben; profitable Trades setzen die Größe zurück auf das Startvolumen, sobald der Kontosaldo ein neues Kapital-Hoch verzeichnet. Die Strategie erzwingt auch einen maximalen schwebenden Drawdown als Prozentsatz des Kontosaldos und schließt jede Position, die diese Schwelle überschreitet.

Die ursprüngliche MQL-Version öffnete Trades auf Ticks. Der StockSharp-Port behält dasselbe probabilistische Verhalten bei, arbeitet aber auf Kerzen-Updates, was sie sowohl für Backtesting als auch für Live-Trading mit Balkendaten geeignet macht.

## Handelsregeln

- **Entscheidungsintervall:** warten Sie auf die angegebene Anzahl abgeschlossener Kerzen, bevor Sie ein neues Signal bewerten. Wenn eine Position noch offen ist, zählt das Intervall weiter, aber kein neuer Trade wird eingegangen.
- **Einstiegsrichtung:** generieren Sie eine Zufallszahl; Werte über 16384 lösen einen Long-Einstieg aus, andernfalls ein Short-Einstieg. Positionen werden nur geöffnet, wenn kein aktiver Trade vorhanden ist.
- **Ordergröße:** starten Sie mit `InitialVolume`. Nach jedem Verlust-Trade wird die nächste Ordergröße zu `PreviousVolume * MartingaleMultiplier` (normalisiert auf den Volumen-Schritt des Instruments). Gewinn-Trades setzen die Größe auf `InitialVolume` zurück, sobald das realisierte Guthaben ein neues Hoch verzeichnet; andernfalls setzt sich die Martingal-Expansion fort.
- **Stops und Ziele:** Stop-Loss und Take-Profit werden in Pips vom Einstiegspreis berechnet. Ein Pip entspricht dem Preis-Schritt des Instruments.
- **Schwebender Drawdown:** während eine Position aktiv ist, überwacht die Strategie das unrealisierte PnL. Wenn der Verlust `MaxDrawdownPercent` des realisierten Kontosaldos (`Anfangsguthaben + realisiertes PnL`) überschreitet, wird die Position sofort geschlossen.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `InitialVolume` | 0.1 | Starthandelsvolumen. |
| `StopLossPips` | 15 | Stop-Abstand in Pips (0 deaktiviert den Stop). |
| `TakeProfitPips` | 30 | Take-Profit-Abstand in Pips (0 deaktiviert das Ziel). |
| `MaxDrawdownPercent` | 75 | Maximal tolerierter schwebender Verlust als Prozent des Guthabens. |
| `MartingaleMultiplier` | 1.6 | Volumen-Multiplikator nach einem Verlust. |
| `DecisionInterval` | 9 | Anzahl abgeschlossener Kerzen zwischen neuen Trade-Entscheidungen. |
| `CandleType` | 1-Minuten-Zeitrahmen | Kerzentyp für die Strategie. |

## Hinweise

- Volumen wird automatisch an die `VolumeStep`-, `MinVolume`- und `MaxVolume`-Grenzen des Instruments normalisiert. Wenn die Normalisierung fehlschlägt, wird die Größe auf das Anfangsvolumen zurückgesetzt.
- Stop-Loss- und Take-Profit-Levels basieren auf dem `PriceStep` des Instruments als ein Pip; überprüfen Sie den Schritt für exotische Symbole.
- Der Drawdown-Schutz erfordert, dass sowohl `PriceStep` als auch `StepPrice` definiert sind; andernfalls wird die Sicherheitsprüfung übersprungen.
- Da die Strategie auf Zufälligkeit beruht, variieren die Ergebnisse zwischen Läufen auch bei identischen Marktdaten, sofern der Zufalls-Seed nicht extern kontrolliert wird.
