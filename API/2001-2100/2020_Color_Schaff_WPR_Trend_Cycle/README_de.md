# Color Schaff WPR Trend-Zyklus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert den **Color Schaff WPR Trend Cycle**-Experten aus MetaTrader.
Sie verwendet den Schaff Trend Cycle, der aus schnellen und langsamen Williams %R-Werten berechnet wird, um Marktwendepunkte zu erkennen.

Der Algorithmus arbeitet ausschließlich mit abgeschlossenen Kerzen. Wenn der Indikatorwert das obere Niveau nach oben kreuzt, eröffnet die Strategie eine Long-Position und schließt alle bestehenden Short-Positionen. Wenn der Wert das untere Niveau nach unten kreuzt, wird eine Short-Position eröffnet und alle bestehenden Long-Positionen werden geschlossen.

## Parameter
- **Fast WPR** – Periode für die Berechnung des schnellen Williams %R.
- **Slow WPR** – Periode für die Berechnung des langsamen Williams %R.
- **Cycle** – Zykluslänge für die Schaff Trend-Berechnung.
- **High Level** – oberes Auslöseniveau für Long-Einstiege.
- **Low Level** – unteres Auslöseniveau für Short-Einstiege.
- **Candle Type** – Zeitrahmen der Kerzen für die Indikatorbewertung.

## Links
- Originale MQL-Quelle: `MQL/13489/mql5/Experts/exp_colorschaffwprtrendcycle.mq5`
- Indikator: `MQL/13489/mql5/Indicators/colorschaffwprtrendcycle.mq5`
