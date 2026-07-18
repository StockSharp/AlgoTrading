# Tick-Delta-Volumen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Analysiert Preis- und Volumenänderungen je Tick. Das Delta wird mit seinem gleitenden Durchschnitt und seiner Standardabweichung verglichen, um einfache Momentum-basierte Einstiege zu generieren.

## Details

- **Einstiegskriterien**: delta > Mittelwert + Standardabw. für Long, delta < -(Mittelwert + Standardabw.) für Short
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegensignal
- **Stops**: Nein
- **Standardwerte**:
  - `Mode` = Volume
  - `Length` = 10
- **Filter**:
  - Kategorie: Volumen
  - Richtung: Beide
  - Indikatoren: EMA, StandardDeviation
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Tick
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
