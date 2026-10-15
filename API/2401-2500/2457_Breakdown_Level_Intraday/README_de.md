# Ausbruch-Level Intraday-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Platziert Ausbruchsorders um das Hoch und Tief des Vortages zu einem bestimmten Zeitpunkt. Long-Einstieg, wenn der Preis über das Hoch plus ein Delta steigt; Short-Einstieg, wenn der Preis unter das Tief minus das Delta fällt. Das Positionsmanagement umfasst optionalen Stop-Loss, Take-Profit, Break-Even und Trailing-Stop.

## Details

- **Einstiegskriterien**:
  - Long: Preis kreuzt über das Vortageshoch + `Delta`
  - Short: Preis fällt unter das Vortagestief − `Delta`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Stop-Loss oder Take-Profit erreicht
  - Trailing-Stop oder Break-Even-Anpassung ausgelöst
- **Stops**: Punkte vom Einstiegspreis
- **Standardwerte**:
  - `OrderTime` = TimeSpan.Zero
  - `Delta` = 6
  - `StopLoss` = 120
  - `TakeProfit` = 90
  - `NoLoss` = 0
  - `Trailing` = 0
  - `Volume` = 1m
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
