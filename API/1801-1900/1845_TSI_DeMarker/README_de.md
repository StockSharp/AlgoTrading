# TSI DeMarker-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den True Strength Index auf Basis des DeMarker-Oszillators berechnet.
Eine Long-Position wird eröffnet, wenn der TSI seine gleitende Durchschnittssignallinie von unten nach oben kreuzt.
Eine Short-Position wird eröffnet, wenn der TSI die Signallinie von oben nach unten kreuzt.

Der Ansatz kombiniert Momentum- und Überkauft-/Überverkauft-Analyse.

## Details

- **Einstiegskriterien**:
  - Long: `TSI kreuzt Signal von unten`
  - Short: `TSI kreuzt Signal von oben`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
  - `DemarkerPeriod` = 25
  - `ShortLength` = 5
  - `LongLength` = 8
  - `SignalLength` = 20
- **Filter**:
  - Kategorie: Oszillator-Crossover
  - Richtung: Beide
  - Indikatoren: TSI, DeMarker
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
