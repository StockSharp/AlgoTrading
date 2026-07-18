# SMC Order Block Zonen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie identifiziert Swing-Hochs und -Tiefs, um Premium- und Discount-Zonen zu definieren. Ein einfacher gleitender Durchschnitt dient als Trendfilter und aktuelle Order-Blocks bestätigen Einstiege. Trades werden ausgeführt, wenn der Preis von einer Zone in Richtung Gleichgewicht mit Order-Block-Bestätigung bewegt, wobei ein prozentualer Stop-Loss zum Schutz verwendet wird.

## Details

- **Einstiegskriterien**:
  - Schlusskurs unter Gleichgewicht, aber über der Discount-Zone und SMA für Long-Trades.
  - Schlusskurs über Gleichgewicht, aber unter der Premium-Zone und SMA für Short-Trades.
  - Der Preis muss das jeweilige Order-Block-Niveau berühren.
- **Long/Short**: Konfigurierbar Long, Short oder beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop-Loss.
- **Stops**: Prozentualer Stop-Loss.
- **Standardwerte**:
  - `SwingHighLength` = 8
  - `SwingLowLength` = 8
  - `SmaLength` = 50
  - `OrderBlockLength` = 20
  - `StopLossPercent` = 2
- **Filter**:
  - Kategorie: Trend und SMC
  - Richtung: Benutzerdefiniert
  - Indikatoren: SMA, Highest, Lowest
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
