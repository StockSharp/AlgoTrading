# Beschreibung der Tief-Ausbruch-Strategie mit Berechnung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Strategieübersicht

Die Strategie „Tief-Ausbruch mit Berechnung" nutzt eine Kombination aus Hoch- und Tief-Kurs-Indikatoren, um potenzielle Ausbruchspunkte am Markt zu identifizieren. Ziel dieser Strategie ist es, Trades auszuführen, wenn der Kurs unter ein berechnetes Tief über einen bestimmten Zeitraum fällt, was auf einen möglichen Abwärtstrend hindeutet.

[![schema](schema.png)](schema_easter_egg.png)

## Strategiedetails

### Komponenten

- **Kerzenbildung**: Verwendet einen Ein-Stunden-Zeitrahmen für die [Kerzengenerierung](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) und erfasst so signifikante Marktbewegungen.
- **Hoch- und Tief-Indikatoren**:
  - **Highest 25**: Verfolgt den [höchsten Kurs](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) über die letzten 25 Perioden.
  - **Lowest 45**: Überwacht den [niedrigsten Kurs](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) über die letzten 45 Perioden.
- **Berechnungslogik**: Bestimmt Trade-Ausführungspunkte durch [Vergleich](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) der aktuellen Kurse mit den aus den Indikatoren berechneten Hoch- und Tief-Niveaus.

### Trade-Ausführung

- **Einstiegssignal**: Eine [Kauf](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Order wird initiiert, wenn der aktuelle Kurs [unter]() den vom Indikator „Lowest 45" berechneten niedrigsten Punkt fällt.
- **Ausstiegssignal**: Eine [Verkauf](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Order wird ausgelöst, wenn die nachfolgende Kursentwicklung die Fortsetzung des Abwärtstrends nicht bestätigt, was durch spezifische Berechnungsparameter definiert wird.

### Visualisierung

- **Chart-Anzeige**: Die Werte der Indikatoren „Highest 25" und „Lowest 45" werden auf dem [Chart](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) neben den Kurskerzen dargestellt und liefern eine visuelle Repräsentation potenzieller Ausbruchspunkte.

## Implementierungsdetails

- **Plattform**: Implementiert auf der StockSharp-Plattform unter Nutzung ihrer Fähigkeiten zur Echtzeit-Datenverarbeitung und Indikatorberechnung.
- **Indikatorverwendung**: Setzt sowohl Hoch- als auch Tief-Indikatoren ein, um einen Bereich festzulegen, innerhalb dessen die Strategie nach Ausbruchspunkten sucht.

## Fazit

Die Strategie „Tief-Ausbruch mit Berechnung" ist für Trader konzipiert, die Chancen auf Basis von Kursausbrüchen aus etablierten Hochs oder Tiefs suchen. Sie kombiniert technische Indikatoren mit ausgefeilter Berechnungslogik, um potenzielle Marktbewegungen zu identifizieren und darauf zu reagieren.
