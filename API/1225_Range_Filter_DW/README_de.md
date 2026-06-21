# Range-Filter-DW-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert einen ATR-basierten Range-Filter ähnlich dem Range Filter von Donovan Wall. Der Filter ignoriert kleine Preisbewegungen und bewegt sich nur, wenn der Preis eine volatilitätsbasierte Bandbreite überschreitet. Eine Long-Position wird eröffnet, wenn der Schlusskurs über dem oberen Band liegt, während eine Short-Position eröffnet wird, wenn der Schlusskurs unter dem unteren Band liegt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs über dem oberen Band.
  - **Short**: Schlusskurs unter dem unteren Band.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Ausbruch des gegenüberliegenden Bandes.
- **Stops**: Nein.
- **Standardwerte**:
  - `RangePeriod` = 14
  - `RangeMultiplier` = 2.618
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: ATR
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
