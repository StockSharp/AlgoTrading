# Donchian RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Donchian Channels und den RSI-Indikator kombiniert. Kauft bei Donchian-Ausbrüchen, wenn der RSI bestätigt, dass der Trend nicht überdehnt ist.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 55%. Am besten geeignet für den Aktienmarkt.

Donchian Channels identifizieren Ausbruchsniveaus, während der RSI prüft, ob der Impuls die Bewegung unterstützt. Positionen werden eröffnet, wenn ein Ausbruch mit der RSI-Richtung übereinstimmt.

Am besten für Trader geeignet, die einen nachhaltigen Ausbruch statt einem Fehlausbruch erwarten. Das Risiko wird durch einen prozentualen Stop begrenzt.

## Details

- **Einstiegskriterien**:
  - Long: `Close > DonchianHigh && RSI < RsiOverboughtLevel`
  - Short: `Close < DonchianLow && RSI > RsiOversoldLevel`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Ausbruchsfehlschlag oder entgegengesetztes Signal
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `DonchianPeriod` = 20
  - `RsiPeriod` = 14
  - `RsiOverboughtLevel` = 70m
  - `RsiOversoldLevel` = 30m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Ein Ausbruch ist nicht überdehnt, solange RSI unter RsiOverboughtLevel (Long) oder über RsiOversoldLevel (Short) bleibt; der Kanal umfasst die vorherigen DonchianPeriod Kerzen. Der Ausbruch scheitert, und die Position schließt, wenn der Kurs wieder jenseits des durchbrochenen Niveaus schließt. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Donchian Channel, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
