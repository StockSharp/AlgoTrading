# ExpBuySellSide-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie konvertiert den MetaTrader Expert Advisor **ExpBuySellSide** in die StockSharp API. Sie kombiniert ein ATR-basiertes Stop-System mit einem vereinfachten Step Up/Down-Trendfilter.

Das ATR-Modul berechnet dynamische Stop-Levels um jede Kerze. Wenn der Preis über das obere Band bricht, gilt der Markt als bullisch; ein Bruch unter das untere Band zeigt eine bärische Phase an.

Das Step Up/Down-Modul vergleicht eine sehr schnelle SMA mit einer langsameren SMA und prüft, ob der Abstand zwischen ihnen zunimmt. Ein wachsender Abstand in Richtung des Crossovers bestätigt den Trend.

Ein Trade wird nur geöffnet, wenn **beide** Module in die gleiche Richtung zeigen. Bestehende Positionen können optional geschlossen werden, wenn ein entgegengesetztes Signal erscheint.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis schließt über dem ATR-Oberband **und** die schnelle SMA entfernt sich von der langsamen SMA nach oben.
  - **Short**: Preis schließt unter dem ATR-Unterband **und** die schnelle SMA entfernt sich von der langsamen SMA nach unten.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal erscheint und die Option *Close Opposite* ist aktiviert.
  - Manueller Stop über Positionsschutz.
- **Stops**: Basierend auf `ATR * Multiplier`-Bändern.
- **Standardwerte**:
  - `ATR Period` = 5.
  - `ATR Multiplier` = 2.5.
  - `Fast SMA` = 2.
  - `Slow SMA` = 30.
  - `Candle Type` = 1-Stunden-Zeitrahmen.
  - `Close Opposite` = true.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

