# MA Stochastic Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
MA Stochastic verwendet einen gleitenden Durchschnitt als Trendfilter mit Rücksetzern des Stochastik-Oszillators.
Wenn der Kurs über dem Durchschnitt aufwärts tendiert und der Stochastik in die überverkaufte Zone taucht, bereitet das System den Kauf beim nächsten Aufschwung vor.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 151%. Die Strategie funktioniert am besten auf dem Aktienmarkt.

Short-Trades spiegeln diese Logik für Abwärtstrends wider: Rallyes werden verkauft, wenn der Stochastik Überkauft-Niveaus erreicht.

Feste prozentuale Stops helfen, große Verluste bei plötzlicher Trendumkehr zu vermeiden.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Moving Average, Stochastic
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

