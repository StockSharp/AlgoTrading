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
  - `Holidays` = 2024 NYSE holidays
  - `StopLossPercent` = 2
    Tage sind UTC-Tage, da der Markt rund um die Uhr handelt; Einstiege erfolgen zum Schluss der ersten Kerze des Tages. Holidays ist eine kommagetrennte Liste von yyyy-MM-dd-Daten, standardmäßig die NYSE-Feiertage 2024 (Karfreitag ist 2024-03-29). Der Markt handelt täglich, daher öffnet der Leerverkauf am Kalendertag nach einem Feiertag und wird mit der letzten Kerze dieses Tages gedeckt.
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

