# Golden Ratio Cubes-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Golden Ratio Cubes-Strategie nutzt Fibonacci-Mathematik zur Erkennung von
Ausbrüchen. Sie verfolgt das Höchsthoch und Tiefsttief über ein Rückblickfenster und
berechnet Erweiterungen basierend auf dem Goldenen Schnitt (φ ≈ 1.618). Wenn der Kurs
über diese Erweiterungen hinaus schließt, tritt die Strategie in die Ausbruchsrichtung
ein.

## Details

- **Einstiegskriterien**:
  - Schlusskurs über der Goldener-Schnitt-Erweiterung der letzten Range → Kaufen.
  - Schlusskurs unter der Goldener-Schnitt-Erweiterung der letzten Range → Verkaufen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Entgegengesetztes Ausbruchssignal.
- **Stops**: Keine.
- **Standardwerte**:
  - `Lookback` = 34
  - `Phi` = 1.618
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long & Short
  - Indikatoren: Highest, Lowest
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
