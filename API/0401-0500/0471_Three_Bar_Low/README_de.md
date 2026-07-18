# 3-Bar-Low-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die 3-Bar-Low-Strategie kauft, wenn der Schlusskurs unter das niedrigste Drei-Bar-Schlusstief fällt, und steigt aus, wenn der Preis über das höchste Sieben-Bar-Schlusshoch schließt. Ein optionaler EMA-Filter kann verlangen, dass der Preis über einem langfristigen Durchschnitt bleibt, bevor Einstiege erlaubt sind.

## Details

- **Einstiegskriterien**:
  - Der Schlusskurs liegt unter dem vorherigen Drei-Bar-Tiefstschlusskurs.
  - Optional: der Schlusskurs liegt über der EMA, wenn der Filter aktiviert ist.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Der Schlusskurs liegt über dem vorherigen Sieben-Bar-Höchstschlusskurs.
- **Stops**: Keine.
- **Standardwerte**:
  - `MaPeriod` = 200
  - `LowestLength` = 3
  - `HighestLength` = 7
  - `UseEmaFilter` = false
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long
  - Indikatoren: EMA, Highest/Lowest
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
