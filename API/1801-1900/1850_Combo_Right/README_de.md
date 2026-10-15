# Combo Right Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie, konvertiert aus dem MQL-Skript `combo_right.mq5`.
Das System kombiniert ein grundlegendes CCI-Signal mit drei einfachen Perzeptronen, die auf Preisdifferenzen arbeiten.

## Logik

1. **Grundsignal** – Wert des Commodity Channel Index (CCI). Positive Werte begünstigen Long-Trades, negative Werte begünstigen Short-Trades.
2. **Perzeptronen** – Jedes Perzeptron betrachtet eine Reihe verschobener Schlusskurse und wendet lineare Gewichte an. Der Modusparameter `Pass` wählt aus, welche Perzeptronen aktiv sind:
   - `1`: nur grundlegendes CCI-Signal.
   - `2`: Verkaufs-Perzeptron kann CCI überschreiben und Short-Positionen eröffnen.
   - `3`: Kauf-Perzeptron kann CCI überschreiben und Long-Positionen eröffnen.
   - `4`: allgemeines Perzeptron überwacht sowohl Kauf- als auch Verkaufs-Perzeptronen.

Wenn ein aktives Perzeptron ein Signal ausgibt, ersetzt es den grundlegenden CCI-Ausgang. Andernfalls wird der CCI-Wert verwendet.

## Parameter

- `TakeProfit1`, `StopLoss1` – Gewinn- und Verlustziele für das grundlegende CCI-Signal (in Ticks).
- `CciPeriod` – Rückblickperiode des CCI-Indikators.
- Gewichte und Perioden für jedes Perzeptron (`x12`, `x22`, …, `p4`).
- `Pass` – Betriebsmodus.
- `Shift` – Balkenindex für Preisdaten (0 aktuell, 1 vorherig).
- `Volume` – Handelsvolumen.
- `CandleType` – Kerzentyp für Berechnungen.

## Indikatoren

- CCI.
