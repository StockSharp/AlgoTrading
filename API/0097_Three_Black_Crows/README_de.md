# Strategie Three Black Crows
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Drei Schwarze Krähen ist das bärische Gegenstück zu Drei Weißen Soldaten und besteht aus drei langen Abwärtskerzen nach einem Aufschwung. Das Muster deutet darauf hin, dass die Verkäufer die Kontrolle übernommen haben, da jeder Schlusskurs nahe dem Sitzungstief liegt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 178%. Die Strategie funktioniert am besten am Aktienmarkt.

Diese Strategie eröffnet eine Short-Position, sobald die dritte Krähe erscheint, und erwartet, dass der Schwung weiter nach unten geht. Sie kann auch verwendet werden, um Longs zu schließen, die von anderen Systemen eröffnet wurden, wenn das Muster am Widerstand entsteht.

Das Risiko wird mit einem engen prozentualen Stop oberhalb des Musterhochs gesteuert, und Trades werden beendet, wenn der Preis wieder über dieses Niveau schließt.

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
