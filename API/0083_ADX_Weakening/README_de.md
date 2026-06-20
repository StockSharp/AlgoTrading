# ADX-Abschwächungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Average Directional Index misst die Trendstärke. Wenn der ADX zu sinken beginnt, signalisiert dies oft, dass die aktuelle Bewegung an Schwung verliert. Dieses System handelt gegen diesen sich abschwächenden Trend, wenn der Preis auf der entgegengesetzten Seite eines einfachen gleitenden Durchschnitts liegt.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 136%. Am besten funktioniert die Strategie am Aktienmarkt.

Für jeden Balken berechnet die Strategie ADX und eine MA. Wenn ADX im Vergleich zum vorherigen Wert sinkt und der Preis über der MA liegt, wird ein Long-Einstieg gesetzt. Fällt ADX, während der Preis unter der MA liegt, wird Short gegangen. Ein fester Stop-Loss schützt die Position.

Da der Ansatz eine Verlangsamung und keine vollständige Umkehr erwartet, werden Trades in der Regel nur gehalten, bis ADX wieder zu steigen beginnt oder der Stop erreicht wird.

## Details

- **Einstiegskriterien**: ADX niedriger als der vorherige Wert und Preis relativ zur MA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `MaPeriod` = 20
  - `StopLoss` = 2%
  - `CandleType` = 15 minute
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: ADX, MA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

