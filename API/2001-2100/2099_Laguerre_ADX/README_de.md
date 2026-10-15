# Laguerre ADX Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wendet einen Laguerre-Filter auf die +DI- und -DI-Komponenten des Average Directional Index (ADX) Indikators an. Die Glättung reduziert Rauschen in der Richtungsbewegung und hebt plötzliche Verschiebungen in der Dominanz zwischen Käufern und Verkäufern hervor. Wenn der Laguerre-geglättete +DI unter den geglätteten -DI kreuzt, tritt das System in eine Long-Position ein und erwartet eine bullische Umkehr. Umgekehrt öffnet das System eine Short-Position, wenn der geglättete +DI über den geglätteten -DI kreuzt.

Positionen werden geschlossen, wenn die aktuellen geglätteten Werte anzeigen, dass die entgegengesetzte Seite die Kontrolle übernommen hat. Die Methode ist als konträrer Ansatz konzipiert und nutzt kurzfristige Extreme im Richtungsindex.

## Details

- **Einstiegskriterien**:
  - **Long**: Laguerre +DI kreuzt unter Laguerre –DI.
  - **Short**: Laguerre +DI kreuzt über Laguerre –DI.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Laguerre –DI bewegt sich über Laguerre +DI.
  - **Short**: Laguerre +DI bewegt sich über Laguerre –DI.
- **Stops**: Keine festen Stops, nur Standard-Positionsschutz.
- **Standardwerte**:
  - `ADX Period` = 14.
  - `Gamma` = 0.764 (Laguerre-Glättungsfaktor).
  - `Candle Type` = 4-Stunden-Zeitrahmen.
- **Filter**:
  - Kategorie: Gegentrend
  - Richtung: Beide
  - Indikatoren: ADX
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
