# Nur-Short 10-Bar-Tief-Pullback-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie geht Short, wenn der Preis das tiefste Tief der vorherigen Bars unterschreitet und die interne Balkenstärke (IBS) über einem Schwellenwert liegt. Ein optionaler EMA-Filter bestätigt den Abwärtstrend.

## Details

- **Einstiegskriterien**:
  - Das Tief unterschreitet das tiefste Tief der vorherigen `LowestPeriod` Bars.
  - IBS > `IbsThreshold`.
  - Optional: Schlusskurs unterhalb des EMA, wenn der Filter aktiviert ist.
  - Uhrzeit zwischen `StartTime` und `EndTime`.
- **Long/Short**: Nur Short.
- **Ausstiegskriterien**:
  - Schlusskurs unter dem vorherigen Tief schließt die Short-Position.
- **Stops**: Keine.
- **Standardwerte**:
  - `LowestPeriod` = 10
  - `IbsThreshold` = 0.85
  - `UseEmaFilter` = true
  - `EmaPeriod` = 200
- **Filter**:
  - Kategorie: Pullback
  - Richtung: Short
  - Indikatoren: Lowest, EMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
