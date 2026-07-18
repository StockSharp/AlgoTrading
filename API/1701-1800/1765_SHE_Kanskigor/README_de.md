# SHE Kanskigor-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese tägliche Strategie eröffnet täglich eine einzige Position basierend auf der Richtung der Kerze des Vortages. Zur konfigurierten Zeit kauft sie, wenn der Vortag unter seinem Eröffnungskurs schloss, und verkauft, wenn er darüber schloss. Ein fester Take-Profit und Stop-Loss, gemessen in Preisschritten, steuern das Risiko. Pro Tag ist nur ein Trade erlaubt.

## Details

- **Einstiegskriterien**: Zur `StartTime` Eröffnungs- und Schlusskurs des Vortages vergleichen; kaufen wenn `open > close`, verkaufen wenn `open < close`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Take-Profit oder Stop-Loss
- **Stops**: Ja
- **Standardwerte**:
  - `Volume` = 0.1
  - `StartTime` = 00:05
  - `TakeProfit` = 350
  - `StopLoss` = 550
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
