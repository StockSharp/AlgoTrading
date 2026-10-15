# BTC-Mining-Schwierigkeitsanpassungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die BTC-Mining-Schwierigkeitsanpassungs-Strategie handelt auf Basis von Änderungen der Bitcoin-Mining-Schwierigkeit. Wenn der Schwellenwertmodus aktiviert ist, werden Trades nur geöffnet, wenn die prozentuale Änderung den angegebenen Schwellenwert überschreitet. Eine Long-Position wird bei positiven Schwierigkeitsanpassungen und eine Short-Position bei negativen Anpassungen eröffnet.

## Details

- **Einstiegskriterien**:
  - Schwellenwertmodus: `abs(change) >= Threshold` und `change < 0` → Long einsteigen.
  - Schwellenwertmodus: `abs(change) >= Threshold` und `change > 0` → Short einsteigen.
  - Ohne Schwellenwertmodus: `difficulty > vorherige difficulty` → Long einsteigen.
  - Ohne Schwellenwertmodus: `difficulty < vorherige difficulty` → Short einsteigen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Das entgegengesetzte Signal schließt und kehrt Positionen um.
- **Stops**: Keine.
- **Standardwerte**:
  - `CandleType` = 1 Tag
  - `ThresholdMode` = false
  - `Threshold` = 10
- **Filter**:
  - Kategorie: Fundamental
  - Richtung: Long & Short
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
