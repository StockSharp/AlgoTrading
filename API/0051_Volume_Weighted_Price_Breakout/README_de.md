# Volume Weighted Price Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie kombiniert einen gleitenden Durchschnitt mit einem volumengewichteten gleitenden Durchschnitt (VWMA). Wenn der Preis über dem VWMA handelt, deutet das darauf hin, dass Käufer dominant sind. Ein Ausbruch tritt auf, wenn der Preis den VWMA von der entgegengesetzten Seite kreuzt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 40%. Am besten funktioniert es im Kryptomarkt.

Trades orientieren sich an der VWMA-Richtung und verwenden den einfachen gleitenden Durchschnitt als übergeordneten Trendfilter. Ausstiege erfolgen, wenn der Preis relativ zum gleitenden Durchschnitt dreht.

Das Ziel ist, Ausbrüche zu erfassen, die durch Volumen unterstützt werden.

## Details

- **Einstiegskriterien**: Preis über oder unter VWMA mit MA-Bestätigung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Preis kreuzt MA in entgegengesetzter Richtung oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `MAPeriod` = 20
  - `VWAPPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: VWMA, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

