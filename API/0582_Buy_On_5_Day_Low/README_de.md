# Kauf am 5-Tage-Tief-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Buy On 5 Day Low**-Strategie geht long, wenn der Schlusskurs unter das vorherige 5-Tage-Tief fällt. Der Ausstieg erfolgt, wenn der Schlusskurs über das Hoch der vorherigen Bar steigt. Der Handel ist auf ein konfigurierbares Zeitfenster begrenzt.

## Details
- **Einstiegskriterien**: Schlusskurs fällt unter das niedrigste Tief der letzten N Kerzen.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Schlusskurs überschreitet das vorherige Hoch.
- **Stops**: Nein.
- **Standardwerte**:
  - `LowestPeriod = 5`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `StartTime = new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero)`
  - `EndTime = new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)`
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: Lowest, High
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
