# BBTrend SuperTrend Decision-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie leitet den **BBTrend**-Wert aus zwei Bollinger-Bändern mit unterschiedlichen Längen ab und speist ihn in eine SuperTrend-Berechnung ein. Die resultierende SuperTrend-Richtung entscheidet, ob Long- oder Short-Positionen eröffnet werden. Optionale prozentbasierte Take-Profit- und Stop-Loss-Schutzmaßnahmen können aktiviert werden.

## Details

- **Einstiegskriterien**:
  - Long: SuperTrend-Richtung ist aufwärts.
  - Short: SuperTrend-Richtung ist abwärts.
- **Long/Short**: Beide, konfigurierbar.
- **Ausstiegskriterien**:
  - Entgegengesetzte SuperTrend-Richtung.
- **Stops**: Optionaler prozentualer TP/SL.
- **Standardwerte**:
  - Kurze BB-Länge = 20, Lange BB-Länge = 50, StdDev = 2.
  - SuperTrend-Länge = 10, Faktor = 7.
  - Take Profit = 30%, Stop Loss = 20%.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, SuperTrend
  - Stops: Optionaler TP/SL
  - Komplexität: Moderat
  - Zeitrahmen: Kurz
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
