# Ichimoku Clouds Long und Short Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie nutzt die Kreuzung von Tenkan-sen und Kijun-sen des Ichimoku-Indikators. Kreuzungen werden je nach Position des Tenkan-Werts relativ zur Wolke als stark, neutral oder schwach klassifiziert. Abhängig vom gewählten Handelsmodus werden Long- oder Short-Positionen eröffnet, wenn die gewählte Signalstärke auftritt. Optionale prozentuale Take-Profit- und Stop-Loss-Werte können Positionen schließen oder entgegengesetzte Signale können dies tun.

## Details

- **Einstiegskriterien**:
  - Tenkan-sen kreuzt über Kijun-sen und die Signalstärke stimmt mit den gewählten Long-Optionen überein.
  - Tenkan-sen kreuzt unter Kijun-sen und die Signalstärke stimmt mit den gewählten Short-Optionen überein.
- **Long/Short**: Konfigurierbar, Standard Long.
- **Ausstiegskriterien**:
  - Entgegengesetzte Signale gemäß den definierten Ausstiegsoptionen.
  - Optionale Take-Profit- oder Stop-Loss-Prozentwerte.
- **Stops**: Prozentuale Take-Profit und Stop-Loss.
- **Standardwerte**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `TakeProfitPct` = 0
  - `StopLossPct` = 0
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Ichimoku
  - Stops: Optional
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
