# Forex Fraus Portfolio-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt ein einzelnes Instrument auf Basis des **Williams %R**-Indikators mit einer langen Periode. Wenn der Indikator extreme Zonen verlässt, eröffnet die Strategie Positionen in Richtung des Ausbruchs.

## Funktionsweise

1. Williams %R wird über `WprPeriod` Kerzen berechnet.
2. Wenn der Indikator unter `BuyThreshold` fällt, wird eine Long-Gelegenheit vorbereitet. Sobald er über den Schwellenwert steigt, wird eine Marktkauforder platziert.
3. Wenn der Indikator über `SellThreshold` steigt, wird eine Short-Gelegenheit vorbereitet. Sobald er unter den Schwellenwert fällt, wird eine Marktverkaufsorder platziert.
4. Positionen sind nur im Zeitfenster zwischen `StartHour` und `StopHour` erlaubt.
5. Optionaler Stop-Loss, Take-Profit und Trailing-Stop können über Parameter aktiviert werden.

## Parameter

- `WprPeriod` – Williams %R-Periode.
- `BuyThreshold` – Wert zur Aktivierung eines Long-Signals.
- `SellThreshold` – Wert zur Aktivierung eines Short-Signals.
- `StartHour` / `StopHour` – Handelssessionsgrenzen.
- `SlPoints` – Stop-Loss in Punkten. Deaktiviert wenn 0.
- `TpPoints` – Take-Profit in Punkten. Deaktiviert wenn 0.
- `UseTrailing` – Trailing-Stop-Logik aktivieren.
- `TrailingStop` – Trailing-Abstand in Punkten.
- `TrailingStep` – Schritt für Trailing-Aktualisierungen.
- `CandleType` – Kerzentyp für die Abonnierung.

## Hinweise

Die ursprüngliche MQL4-Version handelte mehrere Währungspaare und verwaltete Orders für jedes einzelne. Dieser C#-Port konzentriert sich auf ein einzelnes Instrument und demonstriert die Kernidee mithilfe der High-Level-API von StockSharp.
