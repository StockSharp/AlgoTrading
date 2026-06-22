# Laguerre-ROC-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Laguerre-Rate-of-Change-Oszillator, um Trendumkehrungen zu erfassen.

Der Laguerre-ROC-Oszillator glättet die Kursveränderungsrate durch ein vierstufiges Laguerre-Filter.
Die Werte werden zwischen 0 und 1 normalisiert. Zwei Schwellenwerte definieren überkaufte und überverkaufte Zonen:

- **Up Level** – Werte oberhalb dieses Niveaus zeigen starken Aufwärtsschwung an.
- **Down Level** – Werte unterhalb dieses Niveaus zeigen starken Abwärtsschwung an.

Handelslogik:

1. Wenn der Oszillator aus der überkauften Zone fällt (vorheriger Wert über Up Level
   und aktueller Wert darunter) eröffnet die Strategie eine Long-Position.
2. Wenn der Oszillator aus der überverkauften Zone steigt (vorheriger Wert unter Down Level
   und aktueller Wert darüber) eröffnet die Strategie eine Short-Position.
3. Wenn eine Long-Position offen ist und der Oszillator bearish wird (vorheriger Wert unter dem
   neutralen Niveau von 0.5) wird die Position geschlossen.
4. Wenn eine Short-Position offen ist und der Oszillator bullish wird (vorheriger Wert über 0.5)
   wird die Position geschlossen.

Parameter:

- **Period** – Rückblicklänge für die Kursveränderungsberechnung.
- **Gamma** – Glättungsfaktor für das Laguerre-Filter.
- **Up Level** – Überkauft-Schwellenwert.
- **Down Level** – Überverkauft-Schwellenwert.
- **Candle Type** – für Kerzendaten verwendeter Zeitrahmen.

Das Beispiel zeigt, wie benutzerdefinierte Indikatorlogik innerhalb einer High-Level-StockSharp-Strategie
mit integrierter Kursveränderungsrate und manuellem Laguerre-Filtering nachgebaut werden kann.
