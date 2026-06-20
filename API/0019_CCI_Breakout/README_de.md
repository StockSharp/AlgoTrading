# CCI Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf CCI (Commodity Channel Index) Ausbrüchen

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 94%. Am besten funktioniert sie auf dem Aktienmarkt.

CCI Breakout verwendet den Commodity Channel Index, um mächtige Bewegungen zu erkennen. Überschreitungen positiver oder negativer CCI-Schwellenwerte erzeugen Einstiege. Ausstiege erfolgen, wenn CCI sich gegen null zurückzieht oder ein entgegengesetztes Signal entsteht.

Da CCI die Abweichung von einem gleitenden Durchschnitt misst, implizieren extreme Werte unhaltbare Preise. Dieses System wartet auf diese Extreme und versucht dann, vom Folgebewegung zu profitieren.


## Details

- **Einstiegskriterien**: Signale basierend auf CCI, Momentum.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `CciPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: CCI, Momentum
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neural Networks: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

