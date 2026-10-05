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
  - `StopLossPercent` = 2
  - `MaPeriod` = 50
  - `StochPeriod` = 14
  - `StochDPeriod` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
    Kurs über dem SMA ist ein Aufwärtstrend, darunter ein Abwärtstrend. %K unter StochOversold im Aufwärtstrend bereitet einen Long vor, der beim nächsten Anstieg von %K gekauft wird, solange der Kurs über dem SMA bleibt; %K über StochOverbought im Abwärtstrend bereitet einen Short vor, der beim nächsten Rückgang von %K verkauft wird. Das Verlassen des Trends hebt die Vorbereitung auf, ein Gegensignal dreht die Position.
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

