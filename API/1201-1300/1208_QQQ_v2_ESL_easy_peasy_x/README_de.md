# QQQ-Strategie v2 ESL easy-peasy-x
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt QQQ mithilfe eines Crossovers des Schlusskurses mit dem Haupt-MA und Trendfiltern. Sie kauft, wenn der Schlusskurs den Haupt-MA nach oben kreuzt, während der MA steigt und der Kurs über dem langfristigen Trend-MA liegt. Sie verkauft leer, wenn der Schluss den Haupt-MA nach unten kreuzt, während der MA fällt und der Kurs unter dem kurzfristigen Trend-MA liegt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schluss kreuzt über den Haupt-MA, MA-Neigung steigt, Kurs über dem langen Trend-MA.
  - **Short**: Schluss kreuzt unter den Haupt-MA, MA-Neigung fällt, Kurs unter dem kurzen Trend-MA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `Main MA Length` = 200
  - `Trend Long Length` = 100
  - `Trend Short Length` = 50
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Gleitende Durchschnitte
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
