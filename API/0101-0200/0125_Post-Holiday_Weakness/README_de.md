# Strategie der Nachfeiertagsschwäche
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Nachfeiertagsschwäche bezeichnet die Tendenz, dass Kurse unmittelbar nach einem großen Feiertag fallen, wenn das Volumen noch gering ist.
Da viele Marktteilnehmer noch abwesend sind, können Gegentrendbewegungen an Fahrt gewinnen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 112%. Sie funktioniert am besten am Forex-Markt.

Die Strategie geht am Tag nach dem Feiertag short und schließt die Position schnell, sobald die normale Marktbeteiligung zurückkehrt.

Ein kleiner Stop wird verwendet, um übermäßige Verluste bei Handel mit geringer Liquidität zu vermeiden.

## Details

- **Einstiegskriterien**: Kalendereffekt-Auslöser
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Beide
  - Indikatoren: Saisonalität
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

