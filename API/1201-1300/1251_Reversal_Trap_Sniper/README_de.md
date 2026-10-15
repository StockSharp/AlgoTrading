# Reversal Trap Sniper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Reversal Trap Sniper sucht nach RSI-Fallen, bei denen der Momentum zurückgeht, der Preis sich aber weiter bewegt.
Er kauft nach einer überkauften Umkehrung, die dennoch höher schließt, und verkauft nach einer überverkauften Umkehrung, die dennoch tiefer schließt.

## Details

- **Einstiegskriterien**: RSI war vor drei Bars überkauft/überverkauft, aktueller RSI kreuzt zurück und Preis setzt sich in gleicher Richtung fort
- **Long/Short**: Beide
- **Ausstiegskriterien**: ATR-Stop, Ziel oder maximale Anzahl Bars
- **Stops**: ATR-basiert
- **Standardwerte**:
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `RiskReward` = 2
  - `MaxBars` = 30
  - `AtrLength` = 14
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: RSI, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
