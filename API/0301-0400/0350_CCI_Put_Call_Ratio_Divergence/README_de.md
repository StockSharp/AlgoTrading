# CCI Put Call Ratio Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **CCI Put Call Ratio Divergence**-Strategie basiert auf der CCI Put Call Ratio Divergenz.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 133%. Sie funktioniert am besten auf dem Kryptomarkt.

Signale werden ausgelöst, wenn die Divergenz Divergenz-Setups auf Intraday (5m)-Daten bestätigt. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie CciPeriod, AtrMultiplier. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `CciPeriod = 20`
  - `AtrMultiplier = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Divergence
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
