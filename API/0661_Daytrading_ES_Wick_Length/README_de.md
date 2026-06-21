# Daytrading ES Docht-Länge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Long-Position, wenn die gesamte Dochtlänge einer Kerze ihren gleitenden Durchschnitt plus einen Versatz überschreitet, und schließt die Position nach einer festen Anzahl von Bars.

## Details

- **Einstiegskriterien**: Gesamte Dochtlänge größer als gleitender Durchschnitt mit Versatz.
- **Ausstiegskriterien**: Position wird nach dem Halten von `Hold periods` Bars geschlossen.
- **Long/Short**: Nur Long.
- **Stops**: Keine.
- **Standardwerte**:
  - `MA length` = 20
  - `MA type` = VolumeWeighted
  - `MA offset` = 10
  - `Hold periods` = 18
  - `Candle type` = 1-Minuten-Kerzen
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Long
  - Indikatoren: Moving Average, Dochtlänge
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
