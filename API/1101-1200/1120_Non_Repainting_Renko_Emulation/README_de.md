# Nicht-Neuzeichnende Renko-Emulations-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Emuliert Renko-Blöcke anhand von Schlusskursen und handelt Musterübergänge ohne Neuzeichnen.

## Details

- **Einstiegskriterien**:
  - Nach Bildung eines neuen Blocks wird Long gegangen, wenn die vorherige Blockrichtung und Preisfolge eine Aufwärtskontinuation zeigen.
  - Short bei der umgekehrten Sequenz.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Positionen schließen, wenn sich die Blockrichtung umkehrt.
- **Stops**: Nein.
- **Standardwerte**:
  - `BrickSize` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
