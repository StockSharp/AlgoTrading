# Aeron Robot Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein gitterbasiertes Hedging-System, inspiriert vom AeronRobot Expert Advisor. Sie platziert Kauf- und Verkaufsorders in vordefinierten Preisabständen und erhöht das Positionsvolumen nach jeder neuen Order. Der Ansatz zielt darauf ab, kleine Preisschwankungen zu nutzen, während das Risiko durch konfigurierbare Take-Profit-, Stop-Loss- und Trade-Limits gesteuert wird.

Die Strategie arbeitet sowohl mit Long- als auch Short-Positionen. Wenn der Preis sich in durch den *Gap*-Parameter definierten Schritten bewegt, wird eine neue Order mit einem um *LotsFactor* multiplizierten Volumen eröffnet. Gewinne werden gesichert, wenn der Preis um *TakeProfit* Punkte zurückkommt, und Verluste werden begrenzt, wenn die Bewegung *StopLoss* Punkte erreicht. Das *Hedging*-Flag ermöglicht es, Positionen auf beiden Seiten gleichzeitig zu halten.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis fällt um `Gap` Punkte vom letzten Kaufpreis.
  - **Short**: Preis steigt um `Gap` Punkte vom letzten Verkaufspreis.
- **Volumenverwaltung**: Das Volumen jeder neuen Order wird mit `LotsFactor` multipliziert.
- **Ausstiegskriterien**:
  - Positionen einer Seite werden geschlossen, wenn der Gewinn `TakeProfit` Punkte überschreitet.
  - Positionen einer Seite werden geschlossen, wenn der Verlust `StopLoss` Punkte überschreitet.
- **Parameter**:
  - `FirstLot` – anfängliches Ordervolumen.
  - `LotsFactor` – Multiplikator für Folgeorders.
  - `Gap` – Basisabstand zwischen Gitterebenen in Punkten.
  - `GapFactor` – Multiplikator, der den Abstand nach jedem Trade erweitert.
  - `MaxTrades` – maximale Anzahl von Trades pro Seite.
  - `Hedging` – gleichzeitige Long- und Short-Positionen erlauben.
  - `TakeProfit` – Ziel in Punkten.
  - `StopLoss` – Schutzlimit in Punkten.
  - `CandleType` – Kerzen-Zeitrahmen für die Verarbeitung.
- **Long/Short**: beide.
- **Filter**:
  - Kategorie: Grid / Mean Reversion
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch

