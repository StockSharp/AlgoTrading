# Long-Leg Doji Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Long-Leg Doji Ausbruch-Strategie identifiziert langbeinige Doji-Kerzen und handelt Ausbrüche ober- oder unterhalb des Doji-Bereichs. Ein optionaler ATR-Filter stellt sicher, dass die Dochte lang genug sind.

## Details

- **Einstiegskriterien**:
  - **Long**: Warten auf Ausbruch && close > Doji-Hoch && vorheriger close <= Doji-Hoch.
  - **Short**: Warten auf Ausbruch && close < Doji-Tief && vorheriger close >= Doji-Tief.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Close kreuzt SMA(20) entgegen der Position.
- **Stops**: Keine.
- **Standardwerte**:
  - `Doji body threshold %` = 0.1
  - `Minimum wick ratio` = 2
  - `Use ATR filter` = true
  - `ATR period` = 14
  - `ATR multiplier` = 0.5
- **Filter**:
  - Kategorie: Muster-Ausbruch
  - Richtung: Beide
  - Indikatoren: ATR, SMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
