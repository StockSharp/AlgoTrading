# BabyShark VWAP-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert ein volumengewichtetes Durchschnittspreis-Band (VWAP) mit einem OBV-basierten RSI-Filter. Long-Trades entstehen, wenn der Preis unter das untere Abweichungsband fällt und der RSI überverkauft signalisiert. Short-Trades werden ausgelöst, wenn der Preis über das obere Band steigt und der RSI überkauft ist.

Stops verwenden einen kleinen prozentualen Verlust, und Positionen warten eine Abkühlungsperiode vor dem Wiedereinstieg ab.

## Details

- **Einstiegskriterien**: Preis kreuzt Abweichungsbänder mit RSI-Bestätigung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Rückkehr zum VWAP oder Stop-Loss.
- **Stops**: Ja.
- **Standardwerte**:
  - `Length` = 60
  - `RsiLength` = 5
  - `HigherLevel` = 70
  - `LowerLevel` = 30
  - `Cooldown` = 10
  - `StopLossPercent` = 0.6m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: VWAP, RSI, OBV
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
