# Strategie Künstliche Intelligenz Perceptron
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet die Werte des Accelerator Oscillators (AC) als Eingaben für ein einfaches lineares Perceptron. Vier AC-Werte im Abstand von sieben Bars werden mit benutzerdefinierten Koeffizienten gewichtet. Ein positiver Perceptron-Ausgang öffnet eine Long-Position, ein negativer Ausgang öffnet eine Short-Position.

Die Strategie verwendet immer einen Stop-Loss. Wenn nach einem Gewinn, der das Doppelte des Stop-Loss übersteigt, ein entgegengesetztes Signal erscheint, wird die Position mit erhöhtem Volumen umgekehrt. Andernfalls wird der Stop-Loss auf Break-even verschoben.

## Details

- **Einstiegskriterien**:
  - **Long**: Perceptron-Ausgang > 0.
  - **Short**: Perceptron-Ausgang < 0.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal mit Gewinn > 2 * StopLoss → Umkehrung.
  - Entgegengesetztes Signal mit geringerem Gewinn → Stop auf Einstieg verschoben.
  - Stop-Loss ausgelöst.
- **Stops**: Fester Stop-Loss in Punkten.
- **Filter**: Keine.

## Parameter
- `StopLoss` – Stop-Loss-Abstand in Punkten (Standard 850).
- `Shift` – Bar-Versatz für Indikatorwerte (Standard 1).
- `X1`, `X2`, `X3`, `X4` – Perceptron-Gewichte.
- `CandleType` – Kerzen-Zeitrahmen.
