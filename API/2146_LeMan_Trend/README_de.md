# LeMan-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die LeMan-Trend-Strategie leitet bullischen und bearischen Druck aus den jüngsten Hochs und Tiefs ab. Sie berechnet den Abstand zwischen der aktuellen Kerze und den höchsten Hochs sowie den tiefsten Tiefs über drei verschiedene Rückblickperioden. Diese Abstände werden mit einem exponentiellen gleitenden Durchschnitt (EMA) geglättet, um zwei Linien zu bilden: Bullen und Bären. Ein Kreuzungspunkt zwischen diesen Linien signalisiert potenzielle Trendwechsel.

Wenn die Bullenlinie die Bärenlinie von unten nach oben kreuzt, öffnet die Strategie eine Long-Position oder schließt eine bestehende Short-Position. Umgekehrt, wenn die Bärenlinie über die Bullenlinie steigt, öffnet sie eine Short-Position oder verlässt eine Long-Position. Die Methode verwendet keine zusätzlichen Filter und konzentriert sich ausschließlich auf die relative Stärke der jüngsten Hochs und Tiefs.

## Details

- **Einstiegskriterien**
  - **Long**: Bullenlinie kreuzt über die Bärenlinie.
  - **Short**: Bärenlinie kreuzt über die Bullenlinie.
- **Long/Short**: Beide Seiten unterstützt.
- **Ausstiegskriterien**
  - Die entgegengesetzte Kreuzung schließt die aktive Position.
- **Stops**: Standardmäßig keine.
- **Standardwerte**
  - `Min` = 13
  - `Midle` = 21
  - `Max` = 34
  - `EMA period` = 3
  - `Time frame` = 4 hours
- **Filter**
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Highest, Lowest, EMA
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
