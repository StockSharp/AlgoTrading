# Fracture
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Fracture kombiniert fraktale Ausbrüche mit geglätteten gleitenden Durchschnitten und ADX, um sowohl in Seitwärtsmärkten als auch in Trendmärkten zu handeln.

## Details

- **Einstiegskriterien**: Wenn der ADX unter dem Schwellenwert liegt, Long oberhalb des letzten Aufwärtsfraktals oder Short unterhalb des letzten Abwärtsfraktals, wenn der Preis auch ober-/unterhalb der schnellen SMMA liegt. Im Trendregime (schnelle SMMA ober-/unterhalb der langsameren) in Trendrichtung einsteigen, wenn der Preis die schnelle SMMA kreuzt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Position schließen, sobald der Gewinn den ATR multipliziert mit `MinProfit` übersteigt.
- **Stops**: ATR-basiertes Gewinnziel.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `AtrPeriod` = 14
  - `AdxPeriod` = 22
  - `AdxLine` = 40
  - `Ma1Period` = 5
  - `Ma2Period` = 9
  - `Ma3Period` = 22
  - `RangingMultiplier` = 0.5
  - `MinProfit` = 1
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long & Short
  - Indikatoren: Fractal, SMMA, ATR, ADX
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
