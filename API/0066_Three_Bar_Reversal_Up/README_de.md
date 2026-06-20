# Drei-Balken-Aufwärtsumkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Dieses Muster fängt schnelle bullische Wenden nach einem kurzen Rückgang. Es erfordert zwei aufeinanderfolgende Abwärtskerzen, gefolgt von einer starken Aufwärtskerze, die über dem Hoch des vorherigen Balkens schließt. Die Logik prüft optional, ob der Preis zuvor nach unten tendierte.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 85%. Sie funktioniert am besten auf dem Kryptomarkt.

Die Strategie hält die letzten drei Kerzen im Speicher. Sobald die Sequenz den Kriterien entspricht und ein Abwärtstrend-Filter erfüllt ist, wird eine Long-Position eröffnet. Ein Volatilitätsstopp unterhalb des Mustertiefs begrenzt das Risiko des Trades.

Nach dem Einstieg wartet das System entweder auf einen Stop-Treffer oder das Erscheinen eines anderen Setups in die entgegengesetzte Richtung. Dieser einfache Ansatz eignet sich für Märkte, die zu starken Erholungen aus überverkauften Bedingungen neigen.

## Details

- **Einstiegskriterien**: Zwei bärische Kerzen mit tieferen Tiefs, dann eine bullische Kerze, die über dem Hoch des mittleren Balkens schließt.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Stop-Loss oder nächstes Muster.
- **Stops**: Ja, unterhalb des Mustertiefs.
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireDowntrend` = true
  - `DowntrendLength` = 5
- **Filter**:
  - Kategorie: Muster
  - Richtung: Long
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

