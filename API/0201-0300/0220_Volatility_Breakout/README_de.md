# Volatilitäts-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Volatilitäts-Ausbruch-Strategie sucht nach starken gerichteten Bewegungen, wenn der Preis aus seinem durchschnittlichen Bereich ausbricht. Durch die Messung des Abstands von einem einfachen gleitenden Durchschnitt mithilfe des ATR definiert der Algorithmus Ausbruchsschwellen, die mit der Volatilität skalieren.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 97%. Sie funktioniert am besten auf dem Kryptomarkt.

Ein Kaufauftrag wird ausgelöst, wenn der Schlusskurs die SMA um mehr als `Multiplier` mal den ATR übersteigt. Ein Verkaufssignal erscheint, wenn der Schlusskurs um denselben Abstand unter die SMA fällt. Positionen bleiben offen, bis ein entgegengesetzter Ausbruch auftritt oder ein Schutz-Stop getroffen wird.

Diese Technik eignet sich für Intraday-Trader, die von Momentum-Schüben profitieren. ATR-basierte Schwellen helfen, Rauschen herauszufiltern, sodass nur signifikante Bewegungen Trades erzeugen.

## Details
- **Einstiegskriterien**:
  - **Long**: Close > SMA + Multiplier * ATR
  - **Short**: Close < SMA - Multiplier * ATR
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn ein entgegengesetzter Ausbruch ausgelöst wird oder Stop-Loss getroffen wird
  - **Short**: Ausstieg, wenn ein entgegengesetzter Ausbruch ausgelöst wird oder Stop-Loss getroffen wird
- **Stops**: Ja, Stop-Loss bei `Multiplier * ATR` vom Einstieg.
- **Standardwerte**:
  - `Period` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: SMA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
