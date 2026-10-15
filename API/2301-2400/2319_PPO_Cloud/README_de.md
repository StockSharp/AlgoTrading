# PPO Wolken-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Momentum-Strategie handelt Kreuzungen zwischen dem Percentage Price Oscillator (PPO) und seiner Signallinie. Eine Long-Position wird eröffnet, wenn der PPO seine Signallinie nach oben kreuzt, während eine Short-Position bei der entgegengesetzten Kreuzung eröffnet wird. Bestehende Positionen können optional beim gegenteiligen Signal geschlossen werden. Die Strategie arbeitet auf einem einzelnen Zeitrahmen.

## Details

- **Einstiegskriterien**:
  - **Long**: `PPO kreuzt Signal nach oben`.
  - **Short**: `PPO kreuzt Signal nach unten`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - **Long**: `PPO kreuzt Signal nach unten` (optional).
  - **Short**: `PPO kreuzt Signal nach oben` (optional).
- **Stops**: Keine.
- **Standardwerte**:
  - `Fast Period` = 12.
  - `Slow Period` = 26.
  - `Signal Period` = 9.
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
