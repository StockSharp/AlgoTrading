# Dubic EMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt basierend auf der Position des Schlusskurses relativ zu exponentiellen gleitenden Durchschnitten, die über Hochs und Tiefs berechnet werden. Der Handel wird bei engen Spannen und niedrigen Volatilitätsphasen vermieden. Positionen werden durch ATR-basierte Stops, Take-Profit-Level und optionale Parabolic SAR Trailing-Stops abgesichert.

## Details

- **Einstiegskriterien**:
  - **Long**: Close > EMA(High) und Close > EMA(Low), Spannenfilter inaktiv, Volatilität ausreichend.
  - **Short**: Close < EMA(High) und Close < EMA(Low), Spannenfilter inaktiv, Volatilität ausreichend.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Parabolic SAR, ATR/fester Stop-Loss oder Take-Profit.
- **Stops**: Ja.
- **Filter**: Spannen- und Volatilitätsfilter.
