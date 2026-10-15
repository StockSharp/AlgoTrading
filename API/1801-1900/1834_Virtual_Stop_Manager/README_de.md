# Virtueller Stop-Manager
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, konvertiert aus dem MetaTrader-Advisor "VR---STEALS-3-EN". Implementiert versteckte Auftragsverwaltungsfunktionen: Stop-Loss, Take-Profit, Trailing-Stop und Gewinnsicherung. Die Strategie eröffnet beim ersten Kerze eine Long-Position und verwaltet die Ausstiegslevel virtuell, ohne sichtbare Schutzaufträge an der Börse zu platzieren.

## Parameter
- **Volume**: Auftragsvolumen.
- **Take Profit (points)**: Abstand in Punkten zum Schließen der Position mit Gewinn.
- **Stop Loss (points)**: Abstand in Punkten zum Schließen der Position mit Verlust.
- **Trailing Stop (points)**: Abstand des Trailing-Stops vom höchsten Preis.
- **Breakeven (points)**: Gewinn in Punkten, nach dem der Stop-Loss auf den Einstiegspreis verschoben wird.
- **Candle Type**: Kerzenserie, die für die Verarbeitung verwendet wird.
