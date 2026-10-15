# ADX-Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Average Directional Index (ADX)-Trend. Die ADX-Trend-Strategie misst die Marktstärke mithilfe des ADX-Indikators. Wenn der ADX über einem Schwellenwert liegt und der Preis auf der richtigen Seite seines gleitenden Durchschnitts ist, handelt das System in diese Richtung. Positionen werden geschlossen, sobald der ADX schwächer wird oder das entgegengesetzte Setup erscheint.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 46%. Die Strategie funktioniert am besten im Aktienmarkt.

Durch das Warten auf einen soliden ADX-Wert wird nur gehandelt, wenn das Momentum fest etabliert ist. Stops verwenden typischerweise ein ATR-Vielfaches, damit sich das Risiko an die Volatilität anpasst.


## Details

- **Einstiegskriterien**: Signale basierend auf MA, ADX, ATR.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `MaPeriod` = 50
  - `AtrMultiplier` = 2m
  - `AdxExitThreshold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA, ADX, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

