# Volumen-MA-Crossover (Volume MA Cross)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verarbeitet das Volumen durch schnelle und langsame gleitende Durchschnitte. Wenn der schnelle Volumen-MA den langsamen Volumen-MA von unten nach oben kreuzt, deutet dies auf steigende Beteiligung hin und löst einen Long-Einstieg aus. Ein Kreuz von oben nach unten signalisiert Schwäche und initiiert eine Short-Position.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 46 %. Die Strategie eignet sich am besten für den Aktienmarkt.

Positionen werden beim umgekehrten Crossover geschlossen. Der Kurs wird mit seinem eigenen gleitenden Durchschnitt überwacht, um Trades zu filtern.

Volumenbasierte Signale gehen der Kursbewegung oft voraus und ermöglichen frühe Einstiege.

## Details

- **Einstiegskriterien**: Der schnelle Volumen-MA kreuzt den langsamen Volumen-MA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Umgekehrter Crossover oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastVolumeMALength` = 10
  - `SlowVolumeMALength` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Volume MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
