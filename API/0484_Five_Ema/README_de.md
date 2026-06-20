# 5 EMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die 5 EMA-Strategie markiert eine Kerze, die vollständig unter oder über dem 5-Perioden-EMA schließt. Wenn der Preis das Extrem der Signalkerze innerhalb von drei Balken und außerhalb des Blockfensters bricht, tritt die Strategie in Ausbruchsrichtung ein. Ziele basieren auf einem benutzerdefinierten Risiko-Ertrags-Verhältnis und Trades können zu einem bestimmten Zeitpunkt zwangsweise geschlossen werden.

## Details

- **Einstiegskriterien**:
  - Kerzenschluss und Hoch unter EMA → für Long markieren; kaufen wenn der Preis innerhalb von 3 Balken über das Signal-Hoch kreuzt.
  - Kerzenschluss und Tief über EMA → für Short markieren; verkaufen wenn der Preis innerhalb von 3 Balken unter das Signal-Tief kreuzt.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Stop am entgegengesetzten Extrem der Signalkerze.
  - Ziel bei `TargetRR` × Risiko.
  - Optionaler Ausstieg zu benutzerdefinierter Zeit (`ExitHour`, `ExitMinute`).
- **Stops**: Ja.
- **Standardwerte**:
  - `EmaLength` = 5
  - `TargetRR` = 3.0
  - `ExitHour` = 15, `ExitMinute` = 30
  - `BlockStartHour` = 15, `BlockStartMinute` = 0
  - `BlockEndHour` = 15, `BlockEndMinute` = 30
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
