# EMA50 Crossover Monatliche DCA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA50 Crossover Monatliche DCA kauft, wenn der Preis über dem 50-Perioden-EMA schließt, und akkumuliert jeden Monat zusätzliche Positionen. Nicht investierte DCA-Beträge werden als Bargeld gespeichert und eingesetzt, sobald der Trend wieder aufgenommen wird.

Die Strategie verkauft, wenn der Preis unter den EMA fällt und die Position schließt.

## Details

- **Einstiegskriterien**: Schlusskurs > EMA(50)
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Preis kreuzt unter EMA(50)
- **Stops**: Nein
- **Standardwerte**:
  - `CandleType` = 1 Woche
  - `DcaAmount` = 100000
  - `StartDate` = 1980-01-01
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Langfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
