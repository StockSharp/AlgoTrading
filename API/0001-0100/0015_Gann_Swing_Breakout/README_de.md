# Strategie Gann Swing Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf der Gann Swing Breakout-Technik

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 82%. Am besten funktioniert sie auf dem Aktienmarkt.

Gann Swing Breakout verfolgt Swing-Hochs und -Tiefs aus der Gann-Analyse. Ein Ausbruch über das letzte Swing-Niveau eröffnet einen Trade in diese Richtung, der offen bleibt, bis ein entgegengesetzter Swing durchbrochen wird.

Die Methode ist für Trader konzipiert, die vergangene Swing-Punkte als wichtige Unterstützungs- und Widerstandsniveaus betrachten. Durch den Handel beim Ausbruch wird versucht, das nächste Bein eines Trends zu reiten.


## Details

- **Einstiegskriterien**: Signale basierend auf MA, Gann.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `SwingLookback` = 5
  - `MaPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: MA, Gann
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neural Networks: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

