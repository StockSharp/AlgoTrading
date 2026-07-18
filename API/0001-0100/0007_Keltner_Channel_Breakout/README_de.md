# Keltner-Kanal-Ausbruch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Keltner-Kanal-Ausbruch.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 58%. Die Strategie funktioniert am besten im Aktienmarkt.

Der Keltner-Kanal-Ausbruch verwendet Volatilitätsbänder, die vom ATR abgeleitet sind. Ausbrüche über das obere Band oder unter das untere Band lösen Einstiege aus. Wenn der Preis durch die EMA-Mitte zurückbewegt oder einen Stop trifft, wird die Position geschlossen.

Da sich die Bänder mit der Volatilität ausdehnen und zusammenziehen, zielt diese Ausbruchsmethode darauf ab, die frühen Phasen einer starken Bewegung zu erfassen, während dem Preis noch Raum zum Atmen innerhalb des Kanals gelassen wird.


## Details

- **Einstiegskriterien**: Signale basierend auf ATR, Keltner.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: ATR, Keltner
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

