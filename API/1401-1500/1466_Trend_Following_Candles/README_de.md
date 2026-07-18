# Trendfolge-Strategie mit Kerzen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie folgt dem Trend mithilfe eines gleitenden Durchschnitts und einfacher Kerzensignale.
Sie kauft, wenn der Preis über dem gleitenden Durchschnitt liegt und eine bullische Kerze den Pivot-Widerstand durchbricht, und verkauft, wenn der Preis unter dem gleitenden Durchschnitt liegt und eine bearische Kerze den Pivot-Support durchbricht.

## Details

- **Einstiegskriterien**: bullische/bearische Kerze über/unter dem MA, die Pivot-Niveaus durchbricht
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `MaPeriod` = 10
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
