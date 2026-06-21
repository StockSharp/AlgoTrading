# EMA-Prognose-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie auf Basis des EMA-Prognose-Indikators, der Signale erzeugt, wenn sich schnelle und langsame exponentielle gleitende Durchschnitte auf einer richtungsbestätigenden Kerze kreuzen.

Die Strategie eröffnet Long-Positionen, wenn der schnelle EMA den langsamen EMA während einer bullischen Kerze von unten nach oben kreuzt, und schließt dabei alle Short-Positionen. Sie eröffnet Short-Positionen, wenn der schnelle EMA den langsamen EMA während einer bärischen Kerze von oben nach unten kreuzt, und schließt alle Long-Positionen.

## Details

- **Einstiegskriterien**:
  - Long: Schneller EMA kreuzt langsamen EMA von unten nach oben und die Kerze ist bullisch.
  - Short: Schneller EMA kreuzt langsamen EMA von oben nach unten und die Kerze ist bärisch.
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: Fester Take-Profit und Stop-Loss
- **Standardwerte**:
  - `CandleType` = 6-Stunden-Kerzen
  - `FastPeriod` = 1
  - `SlowPeriod` = 2
  - `StopLossTicks` = 1000
  - `TakeProfitTicks` = 2000
- **Filter**:
  - Kategorie: Gleitender-Durchschnitt-Kreuzung
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Take-Profit & Stop-Loss
  - Komplexität: Grundlegend
  - Zeitrahmen: 6-Stunden
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
