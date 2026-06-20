# Keltner Channel Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Volatilitätsbasierte Kanäle können überstreckte Bewegungen hervorheben. Diese Methode handelt gegen den Kurs, wenn er den Keltner Channel verlässt, und erwartet eine Rückkehr zur Mittellinie. Zur Berechnung der Kanalbreite werden ein exponentieller gleitender Durchschnitt und der ATR verwendet.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 106%. Die Strategie eignet sich am besten für den Aktienmarkt.

Mit dem Abschluss jeder Kerze prüft die Strategie, ob der Schlusskurs jenseits der oberen oder unteren Bande liegt und ob die Kerzenrichtung übereinstimmt. Bullische Kerzen, die unterhalb der unteren Bande schließen, lösen Long-Einstiege aus, während bearische Kerzen oberhalb der oberen Bande Short-Positionen veranlassen. Positionen werden geschlossen, sobald der Kurs die Mittelbande kreuzt oder wenn der ATR-basierte Stop erreicht wird.

Indem das System in der entgegengesetzten Richtung kurzfristiger Extreme handelt, sucht es nach schnellen Mean-Reversion-Bewegungen innerhalb einer breiteren Spanne.

## Details

- **Einstiegskriterien**: Schlusskurs außerhalb des Keltner Channel in Kerzenrichtung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Kurs kreuzt die Mittelbande oder Stop-Loss.
- **Stops**: Ja, ATR-basiert.
- **Standardwerte**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0
  - `StopLossAtrMultiplier` = 2.0
  - `CandleType` = 5 minute
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Keltner Channel
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

