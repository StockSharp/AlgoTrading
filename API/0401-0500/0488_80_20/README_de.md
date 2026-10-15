# 80-20-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie erkennt Kerzen, bei denen der Kurs in den oberen oder unteren 20 % der Sitzung schließt. Ein bullisches Signal tritt auf, wenn der Schluss im oberen Fünftel und die Eröffnung im unteren Fünftel der Spanne liegt. Ein bärisches Signal tritt auf, wenn die Eröffnung im oberen Fünftel und der Schluss im unteren Fünftel liegt. Der Ansatz zielt darauf ab, schnelle Umkehrungen von extremen Kerzenschlüssen zu erfassen.

## Details

- **Einstiegskriterien**:
  - Schluss in den oberen 20 % und Eröffnung in den unteren 20 % → Long.
  - Eröffnung in den oberen 20 % und Schluss in den unteren 20 % → Short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Ein entgegengesetztes Signal kehrt die Position um.
- **Stops**: Keine.
- **Standardwerte**:
  - Range percent = 0.2.
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
