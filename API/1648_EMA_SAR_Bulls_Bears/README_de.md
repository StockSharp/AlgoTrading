# EMA SAR Bulls Bears-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen schnellen und langsamen Exponential Moving Average (EMA), Parabolic SAR und Bulls/Bears-Power-Indikatoren. Sie handelt nur während eines konfigurierten Intraday-Fensters und verwendet einfache Gewinn- und Verlustabsicherungen.

Eine Short-Position wird eröffnet, wenn EMA3 unter EMA34 liegt, der Parabolic SAR über dem Kerzenhoch liegt und Bears Power negativ, aber steigend ist. Eine Long-Position wird eröffnet, wenn EMA3 über EMA34 liegt, SAR unter dem Kerzentief liegt und Bulls Power positiv, aber fallend ist.

## Details

- **Einstiegskriterien**:
  - **Long**: EMA3 über EMA34, SAR unter dem Kerzentief, Bulls Power > 0 und abnehmend.
  - **Short**: EMA3 unter EMA34, SAR über dem Kerzenhoch, Bears Power < 0 und zunehmend.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder ausgelöster Stop/Take.
- **Stops**: Ja, absoluter Take-Profit (400 Punkte) und Stop-Loss (2000 Punkte).
- **Filter**:
  - Handelt nur zwischen 09:00 und 17:00.
  - Arbeitet auf 15-Minuten-Kerzen.
