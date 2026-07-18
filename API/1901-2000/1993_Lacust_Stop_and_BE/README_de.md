# Lacust Stop und BE
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie demonstriert ein grundlegendes Positionsmanagement, inspiriert vom ursprünglichen MQL-Expert Advisor **lacuststopandbe**.

Nach dem Einstieg in eine Position in Richtung der zuletzt abgeschlossenen Kerze wendet die Strategie mehrere Schutzregeln an:

- Anfänglicher Stop Loss und Take Profit werden in festen Preisabständen platziert.
- Wenn der Gewinn `BreakevenGain` erreicht, wird der Stop auf den Einstiegspreis plus `Breakeven` verschoben.
- Nachdem der Gewinn `TrailingStart` überschreitet, folgt der Stop dem Preis mit einem Abstand von `TrailingStop`.
- Die Position wird geschlossen, wenn das Stop-Level oder das Take-Profit-Level berührt wird.

Parameter:

- `CandleType` – Kerzenserie für die Verarbeitung.
- `StopLoss` – anfänglicher Stop-Loss-Abstand.
- `TakeProfit` – anfänglicher Take-Profit-Abstand.
- `TrailingStart` – erforderlicher Gewinn zum Aktivieren des Trailing Stops.
- `TrailingStop` – Trailing-Stop-Abstand vom aktuellen Preis.
- `BreakevenGain` – erforderlicher Gewinn vor dem Verschieben des Stops auf Break-Even.
- `Breakeven` – gesicherter Gewinn nach dem Verschieben des Stops auf Break-Even.

Dieses Beispiel verwendet die hochstufige StockSharp-API und kann als Vorlage zum Portieren einfacher MQL-Handelsverwaltungsskripte dienen.
