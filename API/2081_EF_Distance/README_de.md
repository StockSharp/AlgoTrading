# EF Distance Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Adaption des MetaTrader-Expertenberaters "Exp_EF_distance". Sie ersetzt die originalen EF Distance- und Flat-Trend-Indikatoren durch einen einfachen gleitenden Durchschnitt (SMA) und einen Average True Range (ATR)-Filter zur Erkennung von Marktwendepunkten. Der Algorithmus beobachtet drei aufeinanderfolgende SMA-Werte und identifiziert lokale Tiefs oder Hochs. Eine Long-Position wird eröffnet, wenn der SMA ein lokales Tief bildet und die Volatilität den Schwellenwert überschreitet. Eine Short-Position wird beim entgegengesetzten Muster eröffnet. Positionen werden bei entgegengesetzten Signalen oder beim Erreichen von Stop-Loss- oder Take-Profit-Levels geschlossen.

## Details

- **Einstiegskriterien**:
  - **Long**: `SMA(t-1) < SMA(t-2)` und `SMA(t) > SMA(t-1)` und `ATR(t) ≥ AtrThreshold`.
  - **Short**: `SMA(t-1) > SMA(t-2)` und `SMA(t) < SMA(t-1)` und `ATR(t) ≥ AtrThreshold`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Umgekehrtes Signal in entgegengesetzter Richtung.
  - Stop-Loss oder Take-Profit erreicht.
- **Indikatoren**:
  - Einfacher Gleitender Durchschnitt (SMA) – Annäherung an EF Distance.
  - Average True Range (ATR) – Volatilitätsfilter.
- **Standardwerte**:
  - `SMA period` = 10.
  - `ATR period` = 20.
  - `ATR threshold` = 1.
  - `StopLoss` = 100.
  - `TakeProfit` = 200.
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Zwei
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Konfigurierbar
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja (verwendet Wendepunkte)
  - Risikolevel: Mittel
