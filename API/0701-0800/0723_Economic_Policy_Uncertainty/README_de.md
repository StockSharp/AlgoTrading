# Strategie zur wirtschaftspolitischen Unsicherheit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie zur wirtschaftspolitischen Unsicherheit (EPU) geht long, wenn der Zwei-Perioden-SMA des EPU-Index einen benutzerdefinierten Schwellenwert nach oben kreuzt. Nach dem Einstieg wartet die Strategie eine festgelegte Anzahl von Bars, bevor die Position geschlossen wird.

Dieser Ansatz zielt darauf ab, Phasen zu erfassen, in denen die Politikunsicherheit über das normale Niveau steigt.

## Details

- **Einstiegskriterien**: SMA kreuzt den Schwellenwert nach oben.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Ausstieg nach der angegebenen Anzahl von Bars.
- **Stops**: Nein.
- **Standardwerte**:
  - `Threshold` = 187
  - `SmaLength` = 2
  - `ExitPeriods` = 10
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
