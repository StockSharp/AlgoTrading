# ROC Impulce
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf Rate of Change (ROC) Impulsen

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 91%. Am besten funktioniert sie auf dem Aktienmarkt.

ROC Impulse erfasst plötzliche Ausschläge im Rate of Change Indikator. Starke positive Spitzen führen zu Long-Trades und starke negative zu Short-Trades. Wenn das Momentum gegen null nachlässt, wird die Position geschlossen.

Die Auslöseniveaus können so eingestellt werden, dass nur auf außergewöhnliche Momentum-Ereignisse reagiert wird. ATR-basierte Stops helfen, große Verluste zu verhindern, wenn der Ausschlag schnell umkehrt.


## Details

- **Einstiegskriterien**: Signale basierend auf ATR, ROC, Momentum.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `RocPeriod` = 12
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR, ROC, Momentum
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neural Networks: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

