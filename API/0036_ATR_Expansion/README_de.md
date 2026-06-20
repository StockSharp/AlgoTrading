# ATR Expansion Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie folgt Volatilitätsausbrüchen mithilfe des Average True Range. Wenn der ATR im Vergleich zum vorherigen Balken steigt und der Preis relativ zu einem gleitenden Durchschnitt handelt, versucht sie, den Ausbruch zu reiten.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 145%. Es funktioniert am besten auf dem Kryptomarkt.

Die Expansion des ATR deutet auf eine starke Bewegung hin. Einstiege orientieren sich an der Richtung des Preises relativ zum gleitenden Durchschnitt, während Kontraktionen der Volatilität Ausstiege auslösen.

Stops werden mit einem ATR-Vielfachen gesetzt, um Trades bei hoher Volatilität Spielraum zu geben.

## Details

- **Einstiegskriterien**: ATR steigt und Preis über/unter MA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: ATR zieht sich zusammen oder Stop wird erreicht.
- **Stops**: Ja.
- **Standardwerte**:
  - `AtrPeriod` = 14
  - `MAPeriod` = 20
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: ATR, MA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

