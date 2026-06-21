# Ausbruch 04 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Ausbrüche aus der Range des Vortages handelt.
Kauft, wenn der Preis das Tageshoch des Vortages überschreitet, und verkauft, wenn der Preis unter das Tagestief des Vortages fällt.
Verwendet einen Trailing-Stop und einen festen Take-Profit mit optionalem Positionsgrößenmanagement basierend auf dem Kontostand.
Der Handel ist vor einer konfigurierten Montags-Startstunde und nach einer Freitags-Schlusszeit deaktiviert.

## Details

- **Einstiegskriterien**:
  - Long: `Preis > Vorheriges Hoch`
  - Short: `Preis < Vorheriges Tief`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Trailing-Stop oder Take-Profit
- **Stops**: Trailing und fester Stop-Loss
- **Standardwerte**:
  - `MondayHour` = 18
  - `FridayHour` = 14
  - `TrailingStop` = 21
  - `TakeProfit` = 550
  - `StopLoss` = 124
  - `UseMoneyManagement` = false
  - `PercentMM` = 8m
  - `Volume` = 0.1m
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
