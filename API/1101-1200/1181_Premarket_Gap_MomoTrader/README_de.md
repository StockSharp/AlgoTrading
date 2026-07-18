# Premarket Gap MomoTrader Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt einen einzelnen Long-Ausbruch während der Vorbörsensitzung, wenn die aktuelle Kerze mindestens einen angegebenen Prozentsatz über dem vorherigen Schlusskurs gewinnt, eine bullishe Kerze mit ausreichend Volumen druckt und der Kerzenkörper einen großen Teil seiner Spanne einnimmt. Die Positionsgröße wird je nach Körpergröße skaliert.

Nach dem Einstieg hält die Strategie die Position, solange die nächsten Kerzen bullish bleiben und ihr Volumen zunimmt. Eine rote Kerze oder nicht zunehmendes Volumen schließt die Position. Pro Tag ist nur ein Trade erlaubt und der Handel kann auf die Sitzung 04:00–09:30 beschränkt werden.

## Details

- **Einstiegskriterien**:
  - Gewinn der aktuellen Kerze ≥ `MinGainPct` im Vergleich zum vorherigen Schluss.
  - Kerze ist grün und `Volume` > `MinVolume`.
  - Körperprozent definiert Positionsgröße: ≥90% → 100%, ≥85% → 50%, ≥75% → 25%.
  - Optionaler Sitzungsfilter 04:00–09:30 wenn `UseSession` aktiviert ist.
- **Ausstiegskriterien**:
  - Erste rote Kerze oder Kerze mit nicht zunehmendem Volumen nach dem Einstieg.
- **Stops**: Nein.
- **Standardwerte**:
  - `MinGainPct` = 5.
  - `MinVolume` = 15000.
  - `UseSession` = true.
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
