# VRS Vegas Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Umkehrstrategie, die Kerzendochte verwendet.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 37%. Am besten funktioniert sie im Kryptomarkt.

Das System sucht nach großen Spitzen relativ zum Schlusskurs. Ein großer unterer Docht löst einen Long-Einstieg aus, während ein großer oberer Docht einen Short-Einstieg auslöst. Positionen werden geschlossen, wenn sich der Preis um das Doppelte der Spitzengröße in Gewinnrichtung bewegt.

## Details

- **Einstiegskriterien**:
  - **Long**: unterer Docht ≥ Spike% * close und kein oberer Spike.
  - **Short**: oberer Docht ≥ Spike% * close und kein unterer Spike.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Ziel bei Einstieg ± (Spike * 2).
- **Stops**: Nein.
- **Standardwerte**:
  - `SpikePercent` = 0.025
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Price action
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
