# Vier-Bildschirme-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Vier-Bildschirme-Strategie handelt mit Heikin-Ashi-Kerzen über vier Zeitrahmen: 5, 15, 30 und 60 Minuten.
Sie geht Long, wenn alle Zeitrahmen bullische Kerzen zeigen, und Short, wenn alle bärische Kerzen zeigen.
Stop-Loss- und Take-Profit-Niveaus werden in Punkten gesetzt, mit optionalem Trailing-Stop.

## Funktionsweise
1. Abonniert Kerzen-Streams für 5, 15, 30 und 60 Minuten.
2. Berechnet Heikin-Ashi-Eröffnung und -Schluss für jede Kerze.
3. Markiert jeden Zeitrahmen als bullisch oder bärisch.
4. Geht Long, wenn alle bullisch sind; geht Short, wenn alle bärisch sind.
5. Verwendet `StartProtection` zur Anwendung von Stop-Loss, Take-Profit und optionalem Trailing.

## Parameter
- `CandleType` – Basis-Zeitrahmen für 5-Minuten-Kerzen.
- `StopLossPoints` – Stop-Loss-Abstand in Punkten.
- `TakeProfitPoints` – Take-Profit-Abstand in Punkten.
- `UseTrailing` – Trailing-Stop aktivieren (true/false).

Das Handelsvolumen wird durch die `Volume`-Eigenschaft der Strategie definiert.

## Hinweise
- Arbeitet mit der High-Level-API unter Verwendung von `SubscribeCandles` und `Bind`.
- Verarbeitet nur abgeschlossene Kerzen.
- Kommentare im Code sind auf Englisch.
