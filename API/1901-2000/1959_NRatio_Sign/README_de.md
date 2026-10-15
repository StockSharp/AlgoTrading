# NRatio Sign-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den NRatio-Indikator, einen NRTR-basierten Oszillator, der den normalisierten Abstand zwischen dem Preis und einem dynamischen Trailing-Niveau misst. Handelssignale entstehen, wenn NRatio vordefinierte Schwellenwerte kreuzt. Je nach ausgewähltem Modus reagiert das System entweder auf Ausbrüche über die obere und untere Grenze oder auf Rückkehren in die Grenzen.

Der Ansatz kann auf beiden Marktseiten operieren und verwendet prozentbasiertes Risikomanagement für Ausstiege. Die Glättung der Distanzmetrik erfolgt mit einem exponentiellen gleitenden Durchschnitt, was der Strategie ermöglicht, schnell zu reagieren und gleichzeitig Rauschen zu filtern.

## Details

- **Einstiegskriterien**:
  - **Modus In**:
    - **Long**: `NRatio` kreuzt über `UpLevel`.
    - **Short**: `NRatio` kreuzt unter `DownLevel`.
  - **Modus Out**:
    - **Long**: `NRatio` kreuzt über `DownLevel`.
    - **Short**: `NRatio` kreuzt unter `UpLevel`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Schutz-Stop.
- **Stops**: Ja, Take-Profit und Stop-Loss in Prozent.
- **Standardwerte**:
  - `CandleType` = 4-Stunden-Kerzen
  - `Kf` = 1
  - `Length` = 3
  - `Fast` = 2
  - `Sharp` = 2
  - `UpLevel` = 80
  - `DownLevel` = 20
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: NRTR, EMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
