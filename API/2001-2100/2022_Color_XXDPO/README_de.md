# Color XXDPO-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die einen doppelt geglätteten Detrended Price Oscillator verwendet, um Steigungsumkehrungen zu erfassen.

## Details
- **Einstiegskriterien**: Aufwärtsneigung mit steigendem aktuellem Wert eröffnet Long; Abwärtsneigung mit fallendem aktuellem Wert eröffnet Short.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzte Steigungsänderung schließt Positionen.
- **Stops**: Keine.
- **Standardwerte**: Länge der ersten MA 21, Länge der zweiten MA 5, Kerzen-Zeitrahmen 6 Stunden.
- **Filter**: Keine.
