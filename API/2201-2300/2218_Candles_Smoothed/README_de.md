# Strategie mit geglätteten Kerzen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt basierend auf der Farbe geglätteter Kerzen. Für jede abgeschlossene Kerze wird die Differenz zwischen Schluss- und Eröffnungskurs durch einen gleitenden Durchschnitt geführt. Wenn diese geglättete Differenz ihr Vorzeichen ändert, wechselt die Kerzen-"Farbe" und die Strategie dreht ihre Position um.

## Logik

1. Abonnierung einer konfigurierbaren Kerzenserie.
2. Berechnung von `diff = close - open` für jede abgeschlossene Kerze.
3. Glättung des `diff` mit dem ausgewählten gleitenden Durchschnitt.
4. Bestimmung der Kerzenfarbe:
   - **Farbe 0** wenn `smoothed diff > 0` (Schluss über Eröffnung).
   - **Farbe 1** andernfalls.
5. Signalgenerierung:
   - **Kaufen** wenn die Farbe von 0 auf 1 wechselt.
   - **Verkaufen** wenn die Farbe von 1 auf 0 wechselt.
6. Die aktuelle Position wird geschlossen, bevor eine neue eröffnet wird.

## Parameter

- `CandleType` – Zeitrahmen der verarbeiteten Kerzen. Standard ist 1 Stunde.
- `MaLength` – Länge des glättenden gleitenden Durchschnitts. Standard ist 30.
- `MaMethods` – Algorithmus des gleitenden Durchschnitts: `Simple`, `Exponential`, `Smma` oder `Weighted`. Standard ist `Weighted`.

## Hinweise

- Die Strategie verwendet Marktorders über `BuyMarket` und `SellMarket`.
- Die High-Level-API wird für Kerzenabonnements und Diagrammvisualisierung verwendet.
- Indikatorwerte werden über `TryGetValue` abgerufen, um direkte Pufferaufrufe zu vermeiden.
