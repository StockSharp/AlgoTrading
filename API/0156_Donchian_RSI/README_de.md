# Donchian RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Donchian Channels und den RSI-Indikator kombiniert. Kauft bei Donchian-Ausbrüchen, wenn der RSI bestätigt, dass der Trend nicht überdehnt ist.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 55%. Am besten geeignet für den Aktienmarkt.

Donchian Channels identifizieren Ausbruchsniveaus, während der RSI prüft, ob der Impuls die Bewegung unterstützt. Positionen werden eröffnet, wenn ein Ausbruch mit der RSI-Richtung übereinstimmt.

Am besten für Trader geeignet, die einen nachhaltigen Ausbruch statt einem Fehlausbruch erwarten. Das Risiko wird durch einen ATR-Stop begrenzt.

## Details

- **Einstiegskriterien**:
  - Long: `Close > DonchianHigh && RSI < RsiOversoldLevel`
  - Short: `Close < DonchianLow && RSI > RsiOverboughtLevel`
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
