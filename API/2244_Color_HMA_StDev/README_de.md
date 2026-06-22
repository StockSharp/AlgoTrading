# Color HMA StDev-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Hull Moving Average mit einem dynamischen Standardabweichungsfilter.

Das System beobachtet, wie weit der Preis vom HMA abweicht. Wenn der Schlusskurs das
Durchschnittsniveau um ein gewähltes Vielfaches der Standardabweichung überschreitet, eröffnet die Strategie eine Long-Position und umgekehrt für Short-Positionen.
Ein breiterer Multiplikator definiert eine Ausstiegszone, sodass Positionen erst nach einer signifikanten Rückkehr innerhalb der Bande geschlossen werden.

Dieser Ansatz versucht, schnelle Momentum-Impulse zu erfassen und dabei Rauschen zu vermeiden. Der Hull Moving Average reagiert schnell
auf Trendänderungen, und die Standardabweichung passt sich der Volatilität an, sodass die Schwellenwerte in turbulenten
Märkten ausgedehnt werden. Die Strategie handelt in beide Richtungen und verwendet keine festen Stops, sondern verlässt sich auf die
Mean Reversion des Preises zurück zum HMA.

## Details

- **Einstiegskriterien**: Schlusskurs kreuzt HMA ± K1 * StdDev.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Schlusskurs kreuzt HMA ± K2 * StdDev in entgegengesetzter Richtung.
- **Stops**: Kein fester Stop-Loss oder Take-Profit.
- **Standardwerte**:
  - `HmaPeriod` = 13
  - `StdPeriod` = 9
  - `K1` = 1.5m
  - `K2` = 2.5m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend, Volatilität
  - Richtung: Beide
  - Indikatoren: HMA, Standardabweichung
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: 4h
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
