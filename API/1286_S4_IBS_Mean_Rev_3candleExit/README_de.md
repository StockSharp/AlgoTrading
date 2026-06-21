# S4 IBS Mean-Reversion-Strategie mit 3-Kerzen-Ausstieg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kauft, wenn die interne Balkenstärke (IBS) der vorherigen Kerze unter einem Schwellenwert liegt, und erwartet eine Mean Reversion. Sie steigt aus, wenn der Kurs über den Einstiegspreis schließt oder nach drei Kerzen, falls der Trade noch im Verlust liegt.

## Details

- **Einstiegskriterien**: vorheriger IBS <= Schwellenwert
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Schluss über dem Einstiegspreis oder nach 3 Kerzen, falls noch unter dem Einstieg; erzwungener Ausstieg nach Endzeitpunkt
- **Stops**: Nein
- **Standardwerte**:
  - `IbsThreshold` = 0.25
  - `StartTime` = 2024-01-01 05:00:00 UTC
  - `EndTime` = 2024-12-31 00:00:00 UTC
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
