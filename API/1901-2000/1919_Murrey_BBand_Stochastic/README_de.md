# Murrey Math BBand & Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Umkehrungen an extremen Murrey Math-Linien mit Bollinger-Bändern und einem Stochastischen Oszillator als Bestätigung.

Die Methode berechnet Murrey-Niveaus aus den höchsten und niedrigsten Preisen über einen konfigurierbaren Zeitraum. Wenn sich der Preis der 0/8-Linie bei überverkauften Bedingungen nähert, kauft die Strategie. Wenn sich der Preis der 8/8-Linie bei überkauften Bedingungen nähert, verkauft sie. Ein Mindestbreiten-Filter für Bollinger-Bänder verhindert den Handel in flachen Märkten.

## Details

- **Einstiegskriterien**
  - **Long**: Der Schlusskurs liegt innerhalb der *Entry Margin* über der 0/8-Linie, Stochastik <= 21 und Bollinger-Bandbreite >= Schwellenwert.
  - **Short**: Der Schlusskurs liegt innerhalb der *Entry Margin* unter der 8/8-Linie, Stochastik >= 79 und Bollinger-Bandbreite >= Schwellenwert.
- **Long/Short**: Beide.
- **Ausstiegskriterien**
  - Long-Positionen schließen an der 1/8-Linie oder wenn der Preis unter die -2/8-Linie fällt.
  - Short-Positionen schließen an der 7/8-Linie oder wenn der Preis über die +2/8-Linie steigt.
- **Stops**: Murrey-Linien (-2/8 oder +2/8) dienen als Schutz-Stops.
- **Filter**
  - Bollinger-Bandbreiten-Filter.
  - Stochastischer Oszillator-Filter.
