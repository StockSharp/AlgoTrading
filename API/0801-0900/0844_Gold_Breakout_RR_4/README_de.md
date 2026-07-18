# Gold Ausbruch RR4-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Gold Breakout RR4 handelt Donchian-Channel-Ausbrüche bei Gold mit Volumen- und LWTI-Trendfiltern. Es wird nur ein Trade pro Tag innerhalb einer bestimmten Session durchgeführt und ein festes Chance-Risiko-Verhältnis von 4:1 verwendet.

## Details

- **Einstiegskriterien**: Kurs bricht aus dem Donchian-Kanal mit überdurchschnittlichem Volumen und LWTI-Bestätigung innerhalb der Session aus
- **Long/Short**: Beide
- **Ausstiegskriterien**: fester Stop und Ziel nach Chance-Risiko-Verhältnis
- **Stops**: Ja
- **Standardwerte**:
  - `DonchianLength` = 96
  - `MaVolumeLength` = 30
  - `LwtiLength` = 25
  - `LwtiSmooth` = 5
  - `StartHour` = 20
  - `EndHour` = 8
  - `RiskReward` = 4
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Donchian Channel, SMA, WMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
