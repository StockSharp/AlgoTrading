# Volatilitäts-Puls-Strategie mit dynamischem Ausstieg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Momentum-basierte Strategie, die Volatilitätsexpansion erkennt. Einstieg in Richtung des Momentums, wenn ATR über seinem Durchschnitt liegt, Ausstieg mit ATR-basiertem Stop und Take-Profit nach einer Haltedauer.

## Details

- **Einstiegskriterien**: ATR-Volatilitätsexpansion mit Momentum-Bestätigung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss und Take-Profit nach Haltedauer
- **Stops**: ATR-basierter Stop, Take-Profit per Risiko-Ertrags-Verhältnis
- **Standardwerte**:
  - `AtrLength` = 14
  - `MomentumLength` = 20
  - `VolThreshold` = 0.5
  - `MinVolatility` = 1.0
  - `ExitBars` = 42
  - `RiskReward` = 2
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Beide
  - Indikatoren: ATR, SMA, Momentum
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
