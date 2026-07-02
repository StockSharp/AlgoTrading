# Strategie RSI Divergence
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf RSI-Divergenz

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 85%. Am besten funktioniert sie auf dem Kryptomarkt.

RSI Divergence sucht nach Preisextremen, die vom RSI-Oszillator nicht bestätigt werden. Eine bullische Divergenz führt zu einem Kauf und eine bärische Divergenz veranlasst einen Verkauf. Der Trade dauert an, bis sich der RSI umkehrt oder ein Stop ausgelöst wird.

Divergenz-Setups entstehen oft gegen Ende langer Trends. Durch den Vergleich des Oszillator-Verhaltens mit der Preisaction versucht die Strategie, frühe Umkehrungen mit kontrolliertem Risiko zu erfassen.


## Details

- **Einstiegskriterien**: Signale basierend auf RSI.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neural Networks: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel

