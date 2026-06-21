# Kolier SuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Kolier SuperTrend-Indikator, der ATR-Bänder zur Erkennung von Trendumkehrungen verwendet.

Der Indikator zeichnet dynamische Unterstützungs- und Widerstandsniveaus, die vom ATR abgeleitet werden. Eine bullische Umkehr tritt auf, wenn der Preis über dem unteren Band schließt und die Linie unter den Preis kippt. Eine bärische Umkehr tritt auf, wenn der Preis unter dem oberen Band schließt.

Durch das Verfolgen dieses adaptiven Trails versucht die Strategie, starke Trends zu reiten und gleichzeitig geschützt zu bleiben, wenn das Momentum nachlässt.

## Details

- **Einstiegskriterien**: Preis kreuzt die SuperTrend-Linie.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Crossover.
- **Stops**: Nein.
- **Standardwerte**:
  - `Period` = 10
  - `Multiplier` = 3.0m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR, SuperTrend
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Swing (4h)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
