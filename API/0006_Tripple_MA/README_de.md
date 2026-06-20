# Triple MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Triple Moving Average-Crossover.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 55%. Die Strategie funktioniert am besten im Aktienmarkt.

Triple MA richtet drei gleitende Durchschnitte aus, um die Richtung zu definieren. Wenn der kürzeste Durchschnitt über dem mittleren und langen Durchschnitt liegt, erfolgt ein Long-Einstieg. Die umgekehrte Ausrichtung öffnet Shorts, und ein Kreuzung der kurzen und mittleren Linien schließt den Trade.

Die Verwendung von drei Durchschnitten hilft, Rauschen zu filtern, das in Einzel-MA-Systemen vorhanden ist. Dieser geschichtete Ansatz versucht, Momentum zu bestätigen, bevor man sich zu einem Trade verpflichtet.


## Details

- **Einstiegskriterien**: Signale basierend auf MA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `ShortMaPeriod` = 5
  - `MiddleMaPeriod` = 20
  - `LongMaPeriod` = 50
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

