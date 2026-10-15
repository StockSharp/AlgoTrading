# ColorMETRO-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis des ColorMETRO-Indikators, der schnelle und langsame Stufenlinien um den RSI aufbaut.
Eine Long-Position wird eröffnet, wenn die schnelle Linie die langsame Linie von unten kreuzt. Eine Short-Position wird eröffnet, wenn die schnelle Linie die langsame Linie von oben kreuzt. Entgegengesetzte Positionen werden bei denselben Signalen geschlossen.

## Parameter
- **Candle Type** – Kerzentyp für Berechnungen.
- **RSI Period** – Periode für die RSI-Berechnung.
- **Fast Step** – Schrittgröße für die schnelle Linie.
- **Slow Step** – Schrittgröße für die langsame Linie.
- **Stop Loss** – Abstand in Punkten für den Stop-Loss-Schutz.
- **Take Profit** – Abstand in Punkten für den Take-Profit-Schutz.
- **Allow Buy** – Erlaubnis zum Öffnen von Long-Positionen.
- **Allow Sell** – Erlaubnis zum Öffnen von Short-Positionen.
- **Close Long** – Erlaubnis zum Schließen von Long-Positionen.
- **Close Short** – Erlaubnis zum Schließen von Short-Positionen.

Die Strategie verwendet `StartProtection` zur Verwaltung von Stop-Loss- und Take-Profit-Niveaus.
