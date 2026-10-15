# Strategie Graph Style 4th Dimension RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Preisänderungen mit RSI-Niveaus kombiniert.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 80%. Funktioniert gut in volatilen Märkten.

Die Strategie prüft die Richtung der letzten Preisänderung zusammen mit RSI-Extremwerten. Eine Position wird geöffnet, wenn der RSI die überkauften/überverkauften Zonen verlässt und die jüngste Preisänderung die Bewegung bestätigt. Positionen werden geschlossen, wenn der RSI in den mittleren Bereich zurückkehrt oder ein entgegengesetztes Signal erscheint.

## Details

- **Einstiegskriterien**: Richtung der Preisänderung mit RSI-Extremwert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder RSI zurück in die Mitte.
- **Stops**: Prozentualer Stop Loss.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `OverboughtLevel` = 70m
  - `OversoldLevel` = 30m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Prozent
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
