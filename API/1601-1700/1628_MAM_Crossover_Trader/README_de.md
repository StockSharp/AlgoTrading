# MAM Crossover Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die auf dem Vergleich einfacher gleitender Durchschnitte der Schlusskurse und Eröffnungskurse von Kerzen basiert.
Ein Long-Signal tritt auf, wenn die SMA des Schlusskurses über die SMA des Eröffnungskurses kreuzt und der vorherige Balken einen Übergang von unten bestätigt hat. Ein Short-Signal erscheint beim entgegengesetzten Muster. Entgegengesetzte Positionen werden bei Signalumkehr geschlossen. Optionaler fester Stop-Loss und Take-Profit schützen die Trades.

## Details

- **Einstiegskriterien**: Muster von SMA(close)- und SMA(open)-Kreuzungen über die letzten zwei Balken.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzte Kreuzung oder Schutz-Stops.
- **Stops**: Ja.
- **Standardwerte**:
  - `MaPeriod` = 20
  - `StopLossTicks` = 40
  - `TakeProfitTicks` = 190
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Fest
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
