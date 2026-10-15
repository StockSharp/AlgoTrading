# Hoffman Heiken Bias-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hoffman Heiken Bias kombiniert eine Gruppe gleitender Durchschnitte mit einem Heikin Ashi Nettovolumen-Modell, um die Trendrichtung zu bestimmen. Eine Long-Position wird eröffnet, wenn der schnelle SMA über den schnellen EMA steigt, alle längerfristigen Durchschnitte darunter liegen und die Nettovolumen-Regression positiv ist. Shorts werden bei den entgegengesetzten Bedingungen ausgelöst.

## Details

- **Einstiegskriterien**:
  - **Long**: `SMA(5) > EMA(18)` && alle längeren Durchschnitte unter `EMA(18)` && Nettovolumen-Regression > 0.
  - **Short**: `SMA(5) < EMA(18)` && alle längeren Durchschnitte über `EMA(18)` && Nettovolumen-Regression < 0.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `Fast SMA` = 5
  - `Fast EMA` = 18
  - `Net volume length` = 25
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, EMA, ATR, Linear Regression
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
