# Vereinfachte automatische Wachstumsmodell-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie bildet kumulative Durchschnittsbänder aus Hochs und Tiefs und handelt, wenn der Preis diese Levels durchbricht.

## Details

- **Einstiegskriterien**:
  - Schlusskurs über dem oberen Band öffnet eine Long-Position.
  - Schlusskurs unter dem unteren Band öffnet eine Short-Position.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Ein entgegengesetztes Signal schließt die Position.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 10
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Highest, Lowest
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
