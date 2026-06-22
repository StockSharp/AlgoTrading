# Color RSI MACD Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Signale eines MACD-Indikators, der in vier verschiedenen Modi analysiert werden kann:

- **Breakdown** – Handel wenn das MACD-Histogramm die Nulllinie kreuzt.
- **MACD Twist** – Handel wenn die MACD-Linie die Richtung wechselt.
- **Signal Twist** – Handel wenn die Signallinie die Richtung wechselt.
- **MACD Disposition** – Handel bei Kreuzungen zwischen der MACD-Linie und der Signallinie.

Jeder Modus kann Long- und Short-Positionen unabhängig voneinander öffnen oder schließen.

Standardmäßig werden keine Stop-Loss- oder Take-Profit-Niveaus verwendet.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `CandleType` = 4 Stunden
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `Mode` = MACD Disposition
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
