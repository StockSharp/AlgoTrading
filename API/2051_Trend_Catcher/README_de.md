# Trend-Catcher-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Trend Catcher**-Strategie kombiniert den Parabolic SAR mit mehreren einfachen gleitenden Durchschnitten, um Richtungsbewegungen zu erfassen. Sie wartet darauf, dass der Kurs den Parabolic SAR in Richtung der vorherrschenden schnellen Durchschnitte kreuzt, und verwaltet dann die Position mithilfe dynamischer Stop-Loss- und Trailing-Regeln.

Ein Trade wird eröffnet, wenn die neueste Kerze auf der gegenüberliegenden Seite des Parabolic SAR im Vergleich zur vorherigen Kerze schließt, während die schnellen Durchschnitte die Bewegung bestätigen. Der anfängliche Stop-Loss wird aus der Entfernung zum SAR-Punkt berechnet und durch Mindest- und Höchstgrenzen begrenzt. Gewinnziele werden als Vielfaches des Stop-Abstands definiert. Nachdem der Kurs um einen bestimmten Betrag vorgerückt ist, wird der Stop mit einem kleinen Versatz auf den Breakeven verschoben und verfolgt später den Kurs.

## Details

- **Einstiegskriterien**:
  - **Long**: `Close[0] > SAR && Close[1] < SAR_prev && FastMA > SlowMA && Close > FastMA2`.
  - **Short**: `Close[0] < SAR && Close[1] > SAR_prev && FastMA < SlowMA && Close < FastMA2`.
- **Ausstiegskriterien**:
  - Stop-Loss- oder Take-Profit-Level werden erreicht.
  - Trailing Stop nach Erreichen der Gewinnschwelle aktiviert.
  - Ein entgegengesetztes Signal schließt die bestehende Position.
- **Stops**: Dynamischer Stop-Loss basierend auf SAR mit optionalen Breakeven- und Trailing-Anpassungen.
- **Standardwerte**:
  - `SlowMaPeriod = 200`
  - `FastMaPeriod = 50`
  - `FastMa2Period = 25`
  - `SarStep = 0.004`
  - `SarMax = 0.2`
  - `SlMultiplier = 1`
  - `TpMultiplier = 1`
  - `MinStopLoss = 10`
  - `MaxStopLoss = 200`
  - `ProfitLevel = 500`
  - `BreakevenOffset = 1`
  - `TrailingThreshold = 500`
  - `TrailingDistance = 10`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Parabolic SAR, SMA
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
