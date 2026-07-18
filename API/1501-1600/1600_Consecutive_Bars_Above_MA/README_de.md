# Aufeinanderfolgende Bars über MA - Nur-Short-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nur-Short-Strategie, die aufeinanderfolgende Schlusskurse über einem gleitenden Durchschnitt zählt und bei Ausbrüchen über das vorherige Hoch shortet. Ausstieg, wenn der Preis unter das vorherige Tief fällt. Ein optionaler 200 EMA-Filter erzwingt den Abwärtstrend.

## Details

- **Einstiegskriterien**: Schwellenwert aufeinanderfolgender Schlusskurse über MA und Schlusskurs > vorheriges Hoch
- **Long/Short**: Short
- **Ausstiegskriterien**: Schlusskurs unter dem vorherigen Tief
- **Stops**: Nein
- **Standardwerte**:
  - `Threshold` = 3
  - `MaType` = SMA
  - `MaLength` = 5
  - `EmaPeriod` = 200
- **Filter**:
  - Kategorie: Muster
  - Richtung: Short
  - Indikatoren: MA, EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
