# ColorJFatl Digit ReOpen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet einen Jurik Moving Average (JMA) zur Identifizierung der Trendrichtung. Eine Long-Position wird eröffnet, wenn der JMA nach oben dreht und alle Short-Positionen geschlossen werden. Eine Short-Position wird eröffnet, wenn der JMA nach unten dreht und alle Long-Positionen geschlossen werden. Zusätzliche Positionen werden jedes Mal hinzugefügt, wenn der Preis eine feste Anzahl von Punkten in die Handelsrichtung bewegt, bis zu einem Maximum.

## Details

- **Einstieg**:
  - JMA dreht nach oben → Long eröffnen und Shorts schließen.
  - JMA dreht nach unten → Short eröffnen und Longs schließen.
- **Wiedereinstieg**:
  - Nach der Anfangsposition öffnen sich neue Positionen alle `PriceStep` Punkte in die Handelsrichtung bis `MaxPositions` erreicht ist.
- **Ausstieg**:
  - Entgegengesetzter JMA-Dreh schließt aktuelle Positionen.
- **Parameter**:
  - `JmaLength` – JMA-Periode.
  - `PriceStep` – Preisbewegung in Punkten für den Wiedereinstieg.
  - `MaxPositions` – maximale Anzahl gleichzeitiger Positionen.
  - `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` – Aktionen aktivieren oder deaktivieren.
  - `CandleType` – Zeitrahmen für Berechnungen.
- **Indikator**: Jurik Moving Average.
- **Typ**: Trendfolge.
- **Zeitrahmen**: standardmäßig 4 Stunden.
