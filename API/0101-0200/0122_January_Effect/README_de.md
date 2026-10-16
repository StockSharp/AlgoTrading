# Januar-Effekt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Beispiel handelt **wiederkehrende Zeitfenster jedes Monats** mit einem Moving-Average-Filter. Es ist **nicht** auf den klassischen Small-Cap-Januareffekt Ende Dezember/Anfang Januar beschränkt und nutzt keine Unternehmensgrößen- oder Steuerverkaufsdaten. Die zuvor genannten 103% Jahresrendite sind durch BTC/TON nicht belegt.

### Tatsächliche Monatsregeln (C# und Python)

Implementiert ist eine **monatliche** Variante, nicht ausschließlich ein Dezember/Januar-Handel. Bei der ersten zulässigen abgeschlossenen 5-Minuten-Kerze eines neuen Tages 1.–5. wird aus Flat bei `Close > SMA20` gekauft. Am 14.–17. werden Longs geschlossen; aus Flat kann bei `Close < SMA20` ein Short eröffnet werden. Am 26.–31. werden Shorts gedeckt. Aktionen benötigen einen neuen Kalendertag und beachten 50 abgeschlossene Kerzen Wartezeit. Ein nativer Stop-Loss von 2% schützt Long- und Short-Positionen unabhängig von den Datumssignalen. Die Rendite oben ist im BTC/TON-Replay nicht belegt.


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

