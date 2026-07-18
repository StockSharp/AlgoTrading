# ATR-Erschöpfungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein plötzlicher Anstieg der Average True Range weist auf eine sich ausdehnende Volatilität hin, die schnell nachlassen kann. Diese Strategie sucht nach ATR-Werten, die eine gleitende Durchschnittslinie um einen konfigurierbaren Multiplikator überschreiten. In Verbindung mit einer Umkehrkerze zielt sie darauf ab, die anschließende Kontraktion zu erfassen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 139%. Am besten funktioniert die Strategie am Aktienmarkt.

Jeder Balken aktualisiert ATR und seinen eigenen Durchschnitt. Wenn ATR den Durchschnitt um den Multiplikator überschreitet und die Kerze entgegen der vorherigen Bewegung schließt, wird ein Trade eröffnet. Der Stop-Loss verwendet ebenfalls ein ATR-Vielfaches und verankert das Risiko an den aktuellen Volatilitätsniveaus.

Positionen verlassen sich typischerweise auf den Stop für den Ausstieg und suchen eine rasche Gegenbewegung, nachdem der Volatilitätsausbruch abgeklungen ist.

## Details

- **Einstiegskriterien**: ATR-Spike über dem Durchschnitt mit Umkehrkerze.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss.
- **Stops**: Ja, ATR-basiert.
- **Standardwerte**:
  - `AtrPeriod` = 14
  - `AtrAvgPeriod` = 20
  - `AtrMultiplier` = 1.5
  - `MaPeriod` = 20
  - `StopLoss` = 2%
  - `CandleType` = 5 minute
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: ATR, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

