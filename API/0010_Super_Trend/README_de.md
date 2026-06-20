# Super Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Supertrend-Indikator.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 67%. Die Strategie funktioniert am besten im Aktienmarkt.

Super Trend berechnet eine dynamische Linie aus dem ATR, die zwischen Unterstützung und Widerstand wechselt. Ein Preisübergang darüber macht die Tendenz bullisch, und ein Übergang darunter macht sie bärisch. Der Trade endet, wenn die Linie umkehrt.

Indem die Strategie dieser adaptiven Linie folgt, versucht sie, anhaltende Bewegungen zu erfassen und gleichzeitig Fehlsignale zu minimieren. Da das Stop-Level dem Preis folgt, werden Gewinne gesichert, sobald das Momentum nachlässt.


## Details

- **Einstiegskriterien**: Signale basierend auf ATR, Supertrend.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `Period` = 10
  - `Multiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR, Supertrend
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

