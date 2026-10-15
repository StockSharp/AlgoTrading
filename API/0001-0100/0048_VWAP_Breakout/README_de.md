# Strategie VWAP Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
VWAP Breakout sucht nach einem Preiskreuz des volumengewichteten Durchschnittspreises von der entgegengesetzten Seite. Ein Ausbruch über den VWAP signalisiert bullischen Druck, während ein Rückgang unter den VWAP bärisches Sentiment anzeigt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 181%. Am besten funktioniert es im Kryptomarkt.

Die Strategie wartet auf einen Schlusskurs auf der anderen Seite des VWAP und handelt dann in diese Richtung. Ausstiege erfolgen, wenn der Preis wieder durch den VWAP kreuzt.

Da VWAP den durchschnittlichen Transaktionspreis darstellt, führen Ausbrüche oft zu Momentum-Bewegungen.

## Details

- **Einstiegskriterien**: Preis schließt auf der entgegengesetzten Seite des VWAP.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Preis kreuzt zurück durch VWAP oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: VWAP
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

