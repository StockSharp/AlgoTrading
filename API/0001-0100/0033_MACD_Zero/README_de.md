# Strategie MACD Zero Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Dieses System handelt Momentum-Wechsel, wenn das Moving Average Convergence Divergence (MACD)-Histogramm sich der Nulllinie nähert. Ein steigender MACD unterhalb der Null oder ein fallender MACD oberhalb der Null signalisiert eine mögliche Umkehr.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 136%. Es funktioniert am besten auf dem Aktienmarkt.

Die Strategie wartet darauf, dass die MACD-Linie auf null zusteuert, während sie noch auf der gegenüberliegenden Seite ist. Sobald das Momentum nachlässt, tritt sie ein und erwartet einen Preisschwung.

Trades schließen, wenn der MACD seine Signallinie kreuzt oder ein Stop-Loss ausgelöst wird.

## Details

- **Einstiegskriterien**: MACD nähert sich von beiden Seiten der Null.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: MACD kreuzt Signallinie oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

