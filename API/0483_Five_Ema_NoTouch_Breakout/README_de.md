# 5 EMA No-Touch Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die 5 EMA No-Touch Ausbruch-Strategie wartet auf eine Kerze, die vollständig auf einer Seite des 5-Perioden-EMA verbleibt. Wenn der Preis später das Extrem dieser Setup-Kerze bricht, tritt die Strategie in Ausbruchsrichtung ein. Der Stop-Loss wird am entgegengesetzten Extrem platziert und das Take-Profit wird auf ein Vielfaches des Risikos gesetzt.

## Details

- **Einstiegskriterien**:
  - Kerzenhoch unter EMA → Long vorbereiten; eintreten wenn der Preis über das Hoch dieser Kerze bricht.
  - Kerzentief über EMA → Short vorbereiten; eintreten wenn der Preis unter das Tief dieser Kerze bricht.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Stop am Extrem der Setup-Kerze.
  - Ziel bei `RewardRisk` × Risiko.
- **Stops**: Ja.
- **Standardwerte**:
  - `EmaPeriod` = 5
  - `RewardRisk` = 3.0
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long/Short
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: 5 Minuten
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
