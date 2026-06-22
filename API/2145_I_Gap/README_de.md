# I-Gap-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **I-Gap-Strategie** repliziert den MetaTrader-Experten „i-GAP". Sie überwacht die Preislücke zwischen dem Schlusskurs der vorherigen Kerze und dem Eröffnungskurs der aktuellen Kerze. Eine Abwärtslücke, die eine bestimmte Anzahl von Preisschritten überschreitet, kann einen Long-Einstieg auslösen und optional bestehende Short-Positionen schließen. Eine Aufwärtslücke funktioniert genauso für Short-Positionen.

## Details
- **Einstiegskriterien**: Die Eröffnungslücke zwischen aufeinanderfolgenden Kerzen überschreitet die konfigurierte Größe.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Lückensignal.
- **Stops**: Kein fester Stop-Loss oder Take-Profit.
- **Standardwerte**:
  - `CandleType` = 1 hour
  - `GapSize` = 5
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
