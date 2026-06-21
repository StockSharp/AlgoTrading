# Trendlinien-Ausbruch-Strategie mit Multi-Fibonacci-Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie mittelt drei SuperTrend-Berechnungen mit Fibonacci-Multiplikatoren (0.618, 1.618, 2.618) und glättet das Ergebnis mit einer EMA. Dynamische Trendlinien werden aus Swing-Hochs und -Tiefs mit ATR-abgeleiteten Steigungen gebildet. Ein Long-Trade wird eröffnet, wenn der Preis über die obere Trendlinie ausbricht, der geglättete SuperTrend steigt und der +DI-Wert über −DI liegt. Short-Trades spiegeln diese Regeln.

## Details
- **Einstieg**: Trendlinienausbruch mit DMI-Bestätigung und SuperTrend-Übereinstimmung.
- **Ausstieg**: Preis kreuzt zurück über den geglätteten Trend oder erreicht ATR‑basierten Stop/Ziel.
- **Indikatoren**: SuperTrend, ATR, Average Directional Index.
- **Typ**: Ausbruch, Long und Short.
