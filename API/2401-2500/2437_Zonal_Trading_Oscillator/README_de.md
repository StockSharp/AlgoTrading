# Zonal Trading Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Zonal-Trading-Strategie repliziert Bill Williams' klassisches "Zonen"-Konzept. Sie überwacht die Farbe des Awesome Oscillator (AO) und des Accelerator Oscillator (AC). Ein grüner Balken bedeutet, dass der Oszillatorwert im Vergleich zum vorherigen Balken gestiegen ist, ein roter Balken bedeutet, dass er gefallen ist. Wenn beide Oszillatoren grün werden, öffnet die Strategie eine Long-Position. Wenn beide rot werden, öffnet sie eine Short-Position. Jede entgegengesetzte Farbe schließt bestehende Positionen.

## Details
- **Einstiegskriterien**:
  - **Long**: AO steigt und AC steigt.
  - **Short**: AO fällt und AC fällt.
- **Ausstiegskriterien**:
  - **Long**: AO oder AC fällt.
  - **Short**: AO oder AC steigt.
- **Stops**: standardmäßig keine.
- **Parameter**:
  - `AoCandleType` – Zeitrahmen für den Awesome Oscillator (standardmäßig `H4`).
  - `AcCandleType` – Zeitrahmen für den Accelerator Oscillator (standardmäßig `H4`).
  - `BuyOpen`, `SellOpen` – aktivieren oder deaktivieren Long- und Short-Einstiege.
  - `BuyClose`, `SellClose` – aktivieren oder deaktivieren Ausstiege für Long- und Short-Positionen.
- **Indikatoren**: Awesome Oscillator (5/34), Accelerator Oscillator (AO minus SMA(5)).
- **Typ**: Momentum-Folge, funktioniert auf jedem Markt und Zeitrahmen, auf dem die Oszillatoren verfügbar sind.
