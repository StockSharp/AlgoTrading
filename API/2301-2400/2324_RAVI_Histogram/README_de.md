# RAVI Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie konvertiert den MetaTrader RAVI Histogram Expert nach StockSharp. Sie misst die Trendstärke als prozentualen Unterschied zwischen einem schnellen und einem langsamen EMA. Das Ergebnis wird mit oberen und unteren Levels verglichen, um Handelsentscheidungen zu treffen.

Wenn der RAVI-Wert über das obere Level steigt, gilt der Markt als bullish. Short-Positionen werden geschlossen und, wenn aktiviert, eine Long-Position eröffnet. Wenn der Wert unter das untere Level fällt, schließt die Strategie Longs und kann eine Short-Position eröffnen. Standardmäßig arbeitet sie mit Vier-Stunden-Kerzen.

## Details

- **Einstiegskriterien**:
  - **Long**: RAVI kreuzt aufwärts durch `UpLevel`.
  - **Short**: RAVI kreuzt abwärts durch `DownLevel`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Das entgegengesetzte RAVI-Signal schließt bestehende Positionen.
- **Stops**: Keine.
- **Filter**: Keine.
- **Zeitrahmen**: standardmäßig 4-Stunden-Kerzen.
- **Parameter**:
  - `FastLength` und `SlowLength` – EMA-Perioden für die RAVI-Berechnung.
  - `UpLevel` und `DownLevel` – Schwellenwerte zur Definition von Trendzonen.
  - `BuyOpen`, `SellOpen`, `BuyClose`, `SellClose` – aktivieren oder deaktivieren Operationen in jede Richtung.
