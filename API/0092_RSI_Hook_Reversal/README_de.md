# RSI Hook Reversal Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die RSI Hook Reversal-Strategie versucht, kurzfristige Wendepunkte zu erfassen, wenn der RSI eine Extremzone verlässt. Nach einer überkauften oder überverkauften Phase „hakt" der Indikator oft zurück in Richtung der Mittellinie, bevor der Preis reagiert.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 163%. Die Strategie funktioniert am besten am Aktienmarkt.

Die Strategie wartet auf diesen Haken, während der Preis weiterhin in der vorherigen Richtung drückt. Ein Long-Einstieg wird ausgelöst, sobald der RSI aus dem überverkauften Bereich nach oben dreht, während der Preis ein neues Tief markiert; ein Short-Einstieg erfolgt, wenn der RSI aus dem überkauften Bereich nach unten dreht, während ein neues Hoch gebildet wird.

Trades verwenden einen einfachen prozentualen Stop zur Risikosteuerung und schließen typischerweise, wenn der RSI in die entgegengesetzte Richtung hakt.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 Minuten
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
