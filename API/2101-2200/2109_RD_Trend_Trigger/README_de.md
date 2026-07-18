# RD Trend Trigger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die RD Trend Trigger-Strategie verwendet den RD-TrendTrigger-Oszillator, um Trendumkehrungen oder Levelausbrüche je nach gewähltem Modus zu erfassen. Im Twist-Modus folgen Trades den Richtungsänderungen des Oszillators; im Disposition-Modus werden Trades ausgelöst, wenn der Oszillator vordefinierte Levels kreuzt.

## Details

- **Einstiegskriterien**:
  - **Twist-Modus**: Long einsteigen, wenn der Oszillator nach oben dreht; Short einsteigen, wenn er nach unten dreht.
  - **Disposition-Modus**: Long einsteigen, wenn der Oszillator über `HighLevel` steigt; Short einsteigen, wenn er unter `LowLevel` fällt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Gegenteilige Signale oder explizite Ausstiegsbedingungen im Disposition-Modus, wenn der Oszillator über `LowLevel` steigt.
- **Stops**: Standardmäßig keine; Schutz kann extern aktiviert werden.
- **Standardwerte**:
  - `Regress` = 15
  - `T3Length` = 5
  - `T3VolumeFactor` = 0.7
  - `HighLevel` = 50
  - `LowLevel` = -50
  - `Mode` = Twist
  - `CandleType` = 4-hour candles
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: Benutzerdefinierter RD-TrendTrigger (basierend auf Hochs/Tiefs und Tillson T3)
  - Stops: Optional
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
