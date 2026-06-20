# Durchschnittliche-Kraft-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Durchschnittliche-Kraft-Strategie verwendet einen Oszillator, der misst, wo der Schluss innerhalb des höchsten Hochs und niedrigsten Tiefs eines Beobachtungszeitraums liegt, und glättet das Ergebnis mit einem gleitenden Durchschnitt. Positive Werte signalisieren Aufwärtsdruck, während negative Werte Abwärtskraft anzeigen.

Die Strategie geht long, wenn der geglättete Average Force-Wert über null liegt, und short, wenn er unter null liegt.

## Details

- **Einstiegskriterien**:
  - Average Force > 0 → Kaufen.
  - Average Force < 0 → Verkaufen.
- **Long/Short**: Sowohl Long- als auch Short-Positionen.
- **Ausstiegskriterien**:
  - Position kehrt um, wenn Average Force die Nulllinie in die entgegengesetzte Richtung kreuzt.
- **Stops**: Keine.
- **Standardwerte**:
  - `Period` = 18
  - `Smooth` = 6
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Highest, Lowest, SMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
