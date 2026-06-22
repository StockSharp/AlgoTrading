# SAR Automatisierte Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Beispiel demonstriert einen einfachen Handelsansatz basierend auf dem **Parabolic SAR**-Indikator.
Die Strategie eröffnet eine Long-Position, wenn der aktuelle Preis über dem SAR-Wert liegt, und eröffnet eine Short-Position, wenn der Preis unter dem SAR liegt. Weitere Risikomanagementfunktionen umfassen festen Stop-Loss, Take-Profit und einen optionalen Trailing-Stop.

## Parameter
- `SarStep` – Beschleunigungsfaktor für die SAR-Berechnung.
- `SarMax` – maximaler Beschleunigungsfaktor für den SAR.
- `StopLoss` – Stop-Loss-Abstand in Preiseinheiten.
- `TakeProfit` – Take-Profit-Abstand in Preiseinheiten.
- `TrailingStop` – Trailing-Stop-Abstand in Preiseinheiten.
- `CandleType` – Kerzentyp für die Indikatorberechnungen.

## Handelslogik
1. Kerzen abonnieren und Parabolic-SAR-Werte berechnen.
2. **Einstieg**:
   - Long gehen, wenn SAR unter dem Schlusskurs liegt und keine Position besteht.
   - Short gehen, wenn SAR über dem Schlusskurs liegt und keine Position besteht.
3. **Ausstieg**:
   - Position schließen, wenn der Preis das entgegengesetzte SAR-Niveau erreicht.
   - Stop-Loss-, Take-Profit- und Trailing-Stop-Regeln anwenden.

Diese Strategie dient zu Bildungszwecken und zeigt, wie Indikatoren und Risikokontrollen mit der High-Level-API von StockSharp verwendet werden.
