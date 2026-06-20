# Tweezer Bottom Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Tweezer Bottom ist ein Zwei-Kerzen-Umkehrmuster, das nach einem Rückgang erscheint. Beide Kerzen teilen ein ähnliches Tief, was signalisiert, dass Verkäufer nicht über dieses Niveau hinaus drücken konnten.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 184%. Die Strategie funktioniert am besten am Kryptomarkt.

Diese Strategie geht long, nachdem die zweite Kerze den gemeinsamen Boden bestätigt hat, in Erwartung einer Erholung, wenn der Verkaufsdruck nachlässt.

Stops werden knapp unterhalb des gemeinsamen Tiefs platziert, um das Risiko zu steuern, und die Position wird beendet, wenn der Preis keine Erholung zeigt.

## Details

- **Einstiegskriterien**: Mustererkennung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 Minuten
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
