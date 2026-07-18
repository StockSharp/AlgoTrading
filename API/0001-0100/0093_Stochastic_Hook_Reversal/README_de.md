# Strategie Stochastic Hook Reversal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Stochastic Hook Reversal-Strategie beobachtet die %K-Linie auf einen Haken aus dem überkauften oder überverkauften Bereich. Nachdem der Oszillator eine Extremzone erreicht hat, dreht er sich häufig zurück, was darauf hinweist, dass der Schwung nachlässt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 166%. Die Strategie funktioniert am besten am Aktienmarkt.

Das System steigt long ein, wenn %K von unterhalb von zwanzig nach oben dreht, während der Preis ein neues Tief drückt. Es verkauft short, wenn der Oszillator von oberhalb von achtzig nach unten hakt, während eines finalen Aufwärtsschubs.

Positionen verwenden einen kleinen prozentualen Stop und schließen, wenn der Stochastik in die andere Richtung hakt oder der Stop erreicht wird.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 Minuten
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Stochastic
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
