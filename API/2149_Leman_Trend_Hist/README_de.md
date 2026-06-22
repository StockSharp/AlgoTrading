# LeMan-Trend-Hist-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine vereinfachte Konvertierung des ursprünglichen MQL5-Experten „LeManTrendHist". Sie basiert auf einem EMA-basierten Histogramm zur Erzeugung von Handelssignalen.

## Idee

Der ursprüngliche Algorithmus berechnet ein benutzerdefiniertes Histogramm, das aus Preisextremen und geglätteten Spannen abgeleitet wird. Für dieses Beispiel wird das Histogramm durch einen exponentiellen gleitenden Durchschnitt der Kerzenspannen approximiert.

## Strategielogik

1. EMA-Wert für jede abgeschlossene Kerze berechnen.
2. Die letzten drei EMA-Werte vergleichen.
3. Wenn der mittlere Wert niedriger als der älteste ist und der neueste Wert darüber steigt, wird eine Long-Position eröffnet und Short-Positionen geschlossen.
4. Wenn der mittlere Wert höher als der älteste ist und der neueste Wert darunter fällt, wird eine Short-Position eröffnet und Long-Positionen geschlossen.

## Parameter

- **Candle Type** – Zeitrahmen der verarbeiteten Kerzen.
- **EMA Period** – Länge des EMA, der im Platzhalter-Histogramm verwendet wird.
- **Signal Bar** – historischer Versatz für Indikatorwerte (für Kompatibilität beibehalten, in vereinfachter Logik nicht verwendet).
- **Buy/Sell Open** – Long- oder Short-Einstiege aktivieren.
- **Buy/Sell Close** – Schließen bestehender Positionen aktivieren.

## Hinweise

Der echte LeManTrendHist-Indikator verwendet komplexe Glättungsalgorithmen, die noch nicht implementiert sind. Die aktuelle Implementierung dient als Platzhalter und sollte für den Produktionseinsatz durch den vollständigen Indikator ersetzt werden.
