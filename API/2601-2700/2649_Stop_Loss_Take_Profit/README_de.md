# Stop-Loss-Take-Profit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser Port repliziert den MetaTrader-Expertenberater «Stop Loss Take Profit». Die Strategie wirft eine Münze, wenn das Konto flat ist, und öffnet eine Market-Order in der gewählten Richtung. Jede Position erhält sofort pip-basierte Stop-Loss- und Take-Profit-Orders. Wenn der Stop ausgelöst wird, verdoppelt der nächste Trade seine Größe (begrenzt durch die Volumenlimits des Wertpapiers). Ein Take-Profit setzt das Volumen auf den Anfangsbetrag zurück. Das Verhalten spiegelt die ursprüngliche Martingal-Positionsgrößenbestimmung wider und nutzt dabei StockSharp's High-Level-API.

## Handelslogik

- **Marktdaten**: Verwendet den `CandleType`-Parameter (Standard: 1-Minuten-Zeitrahmen), um Entscheidungspunkte zu steuern.
- **Einstiegsregeln**:
  - Wenn `Position == 0` und keine Einstiegsorder ausstehend ist, generiert die Strategie ein pseudozufälliges Boolean.
  - `true` öffnet eine Long-Position mit `BuyMarket(volume)`; `false` öffnet eine Short mit `SellMarket(volume)`.
- **Ausstiegsregeln**:
  - Schutz-Stop-Loss- und Take-Profit-Orders werden platziert, sobald der Einstiegsfill empfangen wird.
  - Ein Stop-Ausstieg verdoppelt die Größe für den nächsten Trade, während ein Take-Profit diese zurücksetzt.
  - Wenn Stop- oder Take-Profit-Abstand auf `0` gesetzt ist, wird die jeweilige Schutzorder übersprungen.
- **Money-Management**:
  - `InitialVolume` definiert die Basisauftragsgröße.
  - Nach einem verlorenen Trade wird die Größe verdoppelt, aber auf `Security.MaxVolume` begrenzt, wenn dieser Wert verfügbar ist.
  - Volumen wird zum `VolumeStep`, `MinVolume` und `MaxVolume` des Instruments normalisiert, damit Orders gültig bleiben.
- **Pip-Verarbeitung**:
  - Standardmäßig leitet die Strategie einen Pip aus `PriceStep` und `Decimals` des Instruments ab (5-stellige FX-Symbole entsprechen 0.0001).
  - Setzen Sie `PipSize` auf einen positiven Wert, um die automatische Pip-Größenerkennung zu überschreiben.

## Parameter

| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `CandleType` | 1-Minuten-Kerzen | Zeitrahmen für Münzwürfe und Einstiege. |
| `StopLossPips` | 1 | Stop-Loss-Abstand in Pips. `0` deaktiviert den Stop. |
| `TakeProfitPips` | 1 | Take-Profit-Abstand in Pips. `0` deaktiviert den Take-Profit. |
| `InitialVolume` | 0.01 | Start-Handelsvolumen. Nach Stop-Loss-Ereignissen verdoppelt, nach Gewinnen zurückgesetzt. |
| `PipSize` | 0 (auto) | Optionale Pip-Größenüberschreibung in absoluten Preiseinheiten. |

## Nutzungshinweise

- Funktioniert auf der Long- und Short-Seite und ist bewusst richtungsneutral.
- Schutzorders werden storniert, sobald die Position geschlossen wird, um veraltete Orders zu vermeiden.
- Der Zufallsgenerator wird mit `Environment.TickCount` geseeded, was bedeutet, dass jede Session unterschiedliche Trade-Sequenzen produziert.
- Geeignet zur Demonstration von Risiko-Layering und Martingal-Verhalten statt für Produktions-Trading ohne weitere Risikokontrollen.
