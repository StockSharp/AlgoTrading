# VIX Trigger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
VIX Trigger reagiert auf Veränderungen des Volatilitätsindex. Ein steigender VIX signalisiert Angst und mögliche Umkehrungen beim zugrunde liegenden Instrument. Die Strategie vergleicht die VIX-Richtung mit dem Preis relativ zu einem gleitenden Durchschnitt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 148%. Es funktioniert am besten auf dem Forex-Markt.

Wenn der VIX steigt und der Preis unter dem gleitenden Durchschnitt liegt, kauft er in Erwartung einer Erholung. Umgekehrt lädt ein steigender VIX mit Preis über dem Durchschnitt zu einer Short-Position ein.

Positionen schließen, wenn der VIX fällt oder der Stop-Loss-Prozentsatz erreicht wird.

## Details

- **Einstiegskriterien**: VIX steigt, während Preis relativ zur MA Long oder Short auslöst.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: VIX fällt oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Contrarian
  - Richtung: Beide
  - Indikatoren: VIX, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

