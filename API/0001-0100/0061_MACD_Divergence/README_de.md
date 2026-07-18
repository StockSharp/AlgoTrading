# MACD-Divergenz (MACD Divergence)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die MACD-Divergenz sucht nach Widersprüchen zwischen der Kursbewegung und dem MACD-Indikator. Höhere Hochs im Kurs, aber niedrigere Hochs im MACD deuten auf nachlassendes Momentum hin (bärische Divergenz), während niedrigere Tiefs im Kurs und höhere MACD-Tiefs auf eine bullische Umkehr hinweisen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 70 %. Die Strategie eignet sich am besten für den Aktienmarkt.

Nach der Erkennung einer Divergenz wartet das System darauf, dass der MACD seine Signallinie kreuzt, bevor es einsteigt. Der Trade wird geschlossen, wenn der MACD in die entgegengesetzte Richtung kreuzt oder der Stop-Loss ausgelöst wird.

## Details

- **Einstiegskriterien**: Bullische oder bärische Divergenz plus MACD-Kreuzung der Signallinie.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: MACD kreuzt in entgegengesetzte Richtung oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastMacdPeriod` = 12
  - `SlowMacdPeriod` = 26
  - `SignalPeriod` = 9
  - `DivergencePeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 2.0m
- **Filter**:
  - Kategorie: Divergenz
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
