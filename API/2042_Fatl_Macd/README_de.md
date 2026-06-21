# FATL MACD Trendstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein Trendfolgesystem auf Basis des **FATL MACD**-Indikators. FATL (Fast Adaptive Trend Line) wird vom Preis subtrahiert, um einen MACD-ähnlichen Oszillator zu erzeugen, der dann durch einen adaptiven gleitenden Durchschnitt geglättet wird. Positive Werte zeigen bullisches Momentum an, negative Werte bearisches Momentum.

Der Algorithmus analysiert die Steigung dieses Oszillators bei jeder abgeschlossenen Kerze:

- Wenn der vorherige Wert niedriger ist als der davor liegende Wert, hat der Oszillator nach oben gedreht. Steigt der aktuelle Wert weiter, öffnet die Strategie eine Long-Position und schließt alle Short-Positionen.
- Wenn der vorherige Wert höher ist als der davor liegende Wert, hat der Oszillator nach unten gedreht. Fällt der aktuelle Wert weiter, öffnet die Strategie eine Short-Position und schließt alle Long-Positionen.

Alle Hauptparameter sind konfigurierbar:

- **Fast EMA** – Periode des schnellen gleitenden Durchschnitts des MACD (Standard 12).
- **Slow EMA** – Periode des langsamen gleitenden Durchschnitts des MACD (Standard 26).
- **Signal EMA** – Periode der Signallinie des MACD (Standard 9).
- **Candle Type** – Kerzenreihe für die Indikatorberechnung.

Positionen werden mit Marktorders eröffnet und geschlossen, wenn ein entgegengesetztes Signal erscheint.
