# MACD CCI Lotfy-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die MACD und CCI mit einem Skalierungsfaktor kombiniert.
Eine Position wird geöffnet, wenn beide Indikatoren extreme Schwellenwerte in dieselbe Richtung kreuzen.

Der MACD-Wert wird mit einem Koeffizienten multipliziert, um die Skala an den CCI anzupassen und einen direkten Vergleich mit demselben Schwellenwert zu ermöglichen.
Der Ansatz zielt darauf ab, Umkehrungen aus überkauften und überverkauften Zonen zu erfassen.

## Details

- **Einstiegskriterien**:
  - Long: `CCI < -Threshold` und `MACD * MacdCoefficient < -Threshold`
  - Short: `CCI > Threshold` und `MACD * MacdCoefficient > Threshold`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Ein entgegengesetztes Signal löst eine umgekehrte Position aus
- **Stops**: Keine
- **Standardwerte**:
  - `CciPeriod` = 8
  - `FastPeriod` = 13
  - `SlowPeriod` = 33
  - `MacdCoefficient` = 86000
  - `Threshold` = 85
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: MACD, CCI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
