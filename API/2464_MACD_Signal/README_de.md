# MACD-Signal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis der Differenz zwischen der MACD-Linie und ihrer Signallinie.
Eine Position wird geöffnet, wenn die Differenz einen ATR-basierten Schwellenwert kreuzt, und bei entgegengesetzten Kreuzungen geschlossen.
Ein Trailing Stop und ein fester Take Profit in Ticks werden angewendet.

## Details

- **Einstiegskriterien**:
  - **Long**: MACD - Signal kreuzt `ATR * Level` von unten nach oben.
  - **Short**: MACD - Signal kreuzt `-ATR * Level` von oben nach unten.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Entgegengesetztes Schwellenwert-Crossing.
- **Stops**:
  - Fester Take Profit in Ticks.
  - Optionaler Trailing Stop.
- **Indikatoren**:
  - MACD (konfigurierbare fast-, slow-, signal-Perioden).
  - ATR(200) zur Skalierung des Schwellenwerts.
