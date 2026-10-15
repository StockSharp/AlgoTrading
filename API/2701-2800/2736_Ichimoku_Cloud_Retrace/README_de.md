# Ichimoku-Cloud-Rückzugs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des MetaTrader-Experten "ichimok2005". Sie sucht nach Rückzügen in die Ichimoku-Cloud und handelt in der Richtung der vorherrschenden Kumo-Steigung. Signale werden nur bei abgeschlossenen Kerzen ausgewertet.

## Überblick

- Funktioniert mit jedem Instrument und Zeitrahmen, der Kerzendaten liefert.
- Verwendet standardmäßig die Standard-Ichimoku-Einstellungen (9/26/52), die jedoch vollständig konfigurierbar sind.
- Handelt sowohl Long als auch Short. Die Positionsgröße wird durch die `Volume`-Eigenschaft der Strategie definiert.
- Optionaler Stop-Loss und Take-Profit können in absoluten Preiseinheiten konfiguriert werden.

## Indikatoren und Parameter

- **Ichimoku**: `Tenkan`-, `Kijun`- und `Senkou Span B`-Längen sind als Parameter verfügbar.
- **Kerzentyp**: Wählen Sie einen beliebigen aggregierten Kerzentyp, der von der Verbindung unterstützt wird (Standard: 1-Stunden-Zeitrahmen).
- **Stop Loss Offset**: Optionaler Abstand unter/über dem Einstiegspreis, der einen Ausstieg erzwingt. Auf `0` setzen, um zu deaktivieren.
- **Take Profit Offset**: Optionaler Gewinnziel-Abstand vom Einstiegspreis. Auf `0` setzen, um zu deaktivieren.

## Einstiegskriterien

### Long-Setup

1. `Senkou Span A` liegt über `Senkou Span B`, was eine bullische Cloud signalisiert.
2. Die aktuelle abgeschlossene Kerze ist bullisch (`Close > Open`).
3. Die Kerze schließt innerhalb der Cloud (`Close` liegt zwischen den beiden Spans).
4. Wenn alle Bedingungen erfüllt sind und die Strategie flach oder short ist, sendet sie eine Market-Kauforder, dimensioniert um jede Short-Exposition zu schließen und eine neue Long-Position zu öffnen.

### Short-Setup

1. `Senkou Span B` liegt über `Senkou Span A`, was eine bärische Cloud signalisiert.
2. Die aktuelle abgeschlossene Kerze ist bärisch (`Open > Close`).
3. Die Kerze schließt innerhalb der Cloud (`Close` liegt zwischen den beiden Spans).
4. Wenn die Bedingungen erfüllt sind und die Strategie flach oder long ist, sendet sie eine Market-Verkaufsorder, dimensioniert um jede Long-Exposition zu schließen und eine neue Short-Position zu öffnen.

## Ausstiegskriterien

- Gegensätzliche Signale kehren die Position automatisch um, indem Schließen und neuer Einstieg in eine einzelne Market-Order kombiniert werden.
- Wenn aktiviert, steigt `Stop Loss Offset` bei `EntryPrice - Offset` für Longs und `EntryPrice + Offset` für Shorts aus, unter Verwendung des Kerzen-Schlusskurses.
- Wenn aktiviert, steigt `Take Profit Offset` bei `EntryPrice + Offset` für Longs und `EntryPrice - Offset` für Shorts aus.
- Manuelles Glätten (Schließen der Strategie) setzt auch den internen Einstiegspreis-Tracker zurück.

## Risikohinweise

- Offsets werden in absoluten Preiseinheiten ausgedrückt. Konvertieren Sie Pip- oder Tick-Abstände vor der Konfiguration in Preis.
- Da die Strategie Kerzen-Schlusskurse für Risikoprüfungen verwendet, sollten bei niedrigeren Zeitrahmen engere Offsets in Betracht gezogen werden.
- Kein Trailing oder Teilausstiege sind implementiert; die Strategie beendet immer die gesamte Position.

## Zusätzliche Implementierungsdetails

- Die Strategie abonniert Kerzen über die High-Level-API und bindet den Ichimoku-Indikator mit `BindEx`.
- Nur abgeschlossene Kerzen lösen Logik aus; Zwischenaktualisierungen werden ignoriert.
- Ein Chart-Bereich wird automatisch erstellt (wenn verfügbar), um Preis, die Ichimoku-Cloud und ausgeführte Trades anzuzeigen.
- `ManageRisk` wird vor der Suche nach neuen Einstiegen ausgeführt, damit Schutzausstiege Priorität haben.
