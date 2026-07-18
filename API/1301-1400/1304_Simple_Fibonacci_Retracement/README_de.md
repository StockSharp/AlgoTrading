# Einfache Fibonacci-Retracement-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet Fibonacci-Retracement-Levels, die aus dem höchsten Hoch und tiefsten Tief über ein Rückblickfenster abgeleitet werden. Wenn der Preis ein ausgewähltes Fibonacci-Level kreuzt, eröffnet die Strategie eine Position und platziert feste pip-basierte Take-Profit- und Stop-Loss-Orders.

## Details

- **Einstieg**: Kreuzung über oder unter dem gewählten Fibonacci-Level.
- **Ausstieg**: Fester Take Profit oder Stop Loss.
- **Indikatoren**: Highest, Lowest.
- **Stops**: Ja.
- **Standardwerte**:
  - `LookbackPeriod` = 100
  - `TakeProfitPips` = 50
  - `StopLossPips` = 20
- **Richtung**: Beide.
