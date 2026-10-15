# Adaptive Trend Flow-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Adaptive Trend Flow-Strategie baut einen volatilitätsbasierten Kanal aus schnellen und langsamen EMAs des typischen Preises. Wenn der Preis die Kanalgrenzen kreuzt, dreht der interne Trend um. Long-Positionen werden eröffnet, wenn der Trend nach oben dreht und optionale SMA- und MACD-Filter zustimmen. Positionen werden geschlossen, wenn der Trend nach unten dreht.

## Details

- **Einstiegskriterien**:
  - Der Trend wechselt von abwärts zu aufwärts und die Filter bestätigen.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Der Trend wechselt von aufwärts zu abwärts.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 2
  - `SmoothLength` = 2
  - `Sensitivity` = 2.0
  - `UseSmaFilter` = true
  - `SmaLength` = 4
  - `UseMacdFilter` = true
  - `MacdFastLength` = 2
  - `MacdSlowLength` = 7
  - `MacdSignalLength` = 2
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: EMA, SMA, MACD, Standard Deviation
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
