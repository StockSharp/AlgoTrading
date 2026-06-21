# KST-Strategie Skyrexio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie geht long, wenn der Know Sure Thing (KST)-Indikator seine Signallinie nach oben kreuzt, während der Kurs über einem gewählten gleitenden Durchschnitt und dem Alligator-Kiefer notiert. Ein Choppiness-Index-Filter kann Einstiege in Seitwärtsmärkten deaktivieren. Positionen werden über ATR-basierte Stop-Loss- und Take-Profit-Niveaus geschlossen.

- **Einstiegskriterien**: KST kreuzt Signal nach oben, Kurs über Filter-MA und Alligator-Kiefer, Choppiness unter Schwellenwert.
- **Ausstiegskriterien**: Kurs erreicht ATR-Stop-Loss oder ATR-Take-Profit.
- **Indikatoren**: KST, ATR, Moving Average, Alligator-Kiefer, Choppiness-Index.

## Parameter
- `CandleType` – Kerzen-Zeitrahmen.
- `AtrStopLoss` – ATR-Multiplikator für Stop-Loss.
- `AtrTakeProfit` – ATR-Multiplikator für Take-Profit.
- `FilterMaType` – Typ des Trendfilter-MA.
- `FilterMaLength` – Länge des Trendfilter-MA.
- `EnableChopFilter` – Choppiness-Filter aktivieren.
- `ChopThreshold` – Schwellenwert des Choppiness-Index.
- `ChopLength` – Periode des Choppiness-Index.
- `RocLen1..4` – ROC-Längen für KST.
- `SmaLen1..4` – SMA-Längen für KST.
- `SignalLength` – SMA-Länge der KST-Signallinie.
