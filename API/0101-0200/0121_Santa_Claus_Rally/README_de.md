# Santa Claus Rally Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Implementierung handelt **Kalendertage jedes Monats**, nicht ausschließlich die traditionelle Phase Ende Dezember/Anfang Januar. Die bisherige Angabe einer jährlichen Rendite von 100% ist durch den BTC/TON-Replay nicht belegt.

### Tatsächliche Monatsregeln (C# und Python)

Dieses Beispiel **überträgt** den Santa-Claus-Effekt auf **jeden Monat**; es handelt nicht nur im Dezember/Januar. Bei der ersten zulässigen abgeschlossenen 5-Minuten-Kerze eines neuen Tages zwischen dem 25. und 31. wird aus Flat Long eröffnet oder ein Short gedeckt. Am 3.–7. werden Longs geschlossen, am 12.–18. darf bei `Close < SMA20` aus Flat ein Short eröffnet werden. Handlungen benötigen einen neuen Tag und berücksichtigen 50 abgeschlossene Kerzen Pause. Ein nativer Stop-Loss von 2% schützt ausgeführte Positionen unabhängig vom Kalender. Die oben genannte Rendite ist im BTC/TON-Replay nicht bestätigt.


## Details

- **Einstiegskriterien**: Kalendereffekt-Auslöser
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2%
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Beide
  - Indikatoren: Saisonalität
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

