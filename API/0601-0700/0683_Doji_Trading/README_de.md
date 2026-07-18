# Doji-Handels-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie sucht nach Doji-Kerzen, die oberhalb eines exponentiellen gleitenden Durchschnitts auftreten. Wenn ein solches Muster auftritt, wird eine Long-Position eröffnet. Der Stop-Loss wird auf das niedrigste Tief der letzten Balken gesetzt, und ein Trailing-Stop schützt den Gewinn, nachdem sich der Preis ausreichend in die gewünschte Richtung bewegt hat.

## Details

- **Einstiegskriterien**: Doji-Kerze mit Schluss oberhalb der EMA.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Stop auf dem niedrigsten Tief und Trailing-Stop.
- **Stops**: Ja, fest und Trailing.
- **Standardwerte**:
  - `CandleType` = 5 Minuten
  - `EmaLength` = 60
  - `Tolerance` = 0.05
  - `StopBars` = 450
  - `TrailTriggerPercent` = 1
  - `TrailOffsetPercent` = 0.5
- **Filter**:
  - Kategorie: Muster
  - Richtung: Long
  - Indikatoren: EMA, Kerzenmuster
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
