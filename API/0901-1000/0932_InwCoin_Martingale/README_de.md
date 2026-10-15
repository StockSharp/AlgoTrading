# InwCoin Martingale-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert einen einfachen Martingale-Ansatz für Long-Positionen auf Bitcoin.
Sie unterstützt drei optionale Einstiegssignale: MACD-Histogramm kreuzt über null,
Stochastic RSI %D kreuzt über das Niveau 20, oder der Kurs bricht einen ATR-basierten Kanal.
Nach jedem Kauf kann die Positionsgröße verdoppelt werden, wenn der Kurs um einen konfigurierten Prozentsatz fällt.
Die gesamte Position wird geschlossen, wenn der Gewinn einen festgelegten Prozentsatz über dem durchschnittlichen Einstiegspreis erreicht.

## Details

- **Einstiegssignale**
  - **MACD Line > 0**: Histogramm kreuzt über null.
  - **STO RSI cross up**: %D-Linie kreuzt über 20, während %K in der überverkauften Zone liegt.
  - **ATR Channel**: Schlusskurs kreuzt über EMA plus ATR-Multiplikator.
- **Take profit**: Position wird geschlossen, wenn der Kurs den Durchschnittspreis um den konfigurierten Prozentsatz übersteigt.
- **Martingale**: Zusätzliche Käufe erfolgen, wenn der Kurs um den konfigurierten Prozentsatz vom Durchschnittspreis fällt.
- **Richtung**: Nur Long.

