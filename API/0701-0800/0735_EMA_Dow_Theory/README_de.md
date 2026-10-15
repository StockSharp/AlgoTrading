# EMA-Dow-Theorie-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen Kreuzung einer schnellen und langsamen Exponentiellen Gleitenden Durchschnitts (EMA) mit einem grundlegenden Trendfilter nach der Dow-Theorie. Der Trend wird durch aktuelle Swing-Hochs und -Tiefs bestimmt. Positionen werden eröffnet, wenn die EMAs mit der Trendrichtung übereinstimmen.

## Details

- **Einstiegskriterien**:
  - **Long**: Schnelle EMA ≥ langsame EMA und Preis bricht über das letzte Swing-Hoch.
  - **Short**: Schnelle EMA < langsame EMA und Preis bricht unter das letzte Swing-Tief.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - Länge schnelle EMA = 47
  - Länge langsame EMA = 50
  - Swing-Länge = 6 Bars
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, Swing-Hoch/-Tief
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
