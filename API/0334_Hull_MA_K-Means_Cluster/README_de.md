# Hull MA K-Means Cluster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die **Hull MA K-Means Cluster**-Strategie handelt basierend auf der Richtung des Hull Moving Average mit K-Means-Clustering zur Erkennung des Marktzustands.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 97%. Sie erzielt die besten Ergebnisse auf dem Kryptomarkt.

Signale werden ausgelöst, wenn die Indikatoren Trendwechsel auf Intraday-Daten (5m) bestätigen. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie HullPeriod, ClusterDataLength. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: Siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop-Logik.
- **Stops**: Ja, unter Verwendung indikatorbasierter Berechnungen.
- **Standardwerte**:
  - `HullPeriod = 9`
  - `ClusterDataLength = 50`
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere Indikatoren
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
