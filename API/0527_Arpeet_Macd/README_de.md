# Arpeet MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Arpeet MACD-Strategie handelt MACD-Kreuzungen mit einem Nulllinien-Filter. Ein Long-Signal erscheint, wenn die MACD-Linie die Signallinie von unten kreuzt und dabei unterhalb von null bleibt. Ein Short-Signal tritt auf, wenn der MACD die Signallinie von oben kreuzt und dabei oberhalb von null liegt.

## Details

- **Einstiegskriterien**:
  - **Long**: MACD kreuzt über die Signallinie und MACD < 0.
  - **Short**: MACD kreuzt unter die Signallinie und MACD > 0.
- **Stops**: Keine.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
- **Filter**:
  - Kategorie: Indikator
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
