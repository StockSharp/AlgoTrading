# Januar-Barometer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Januar-Barometer besagt, dass die Marktentwicklung im Januar den Ton für den Rest des Jahres vorgibt. Diese Strategie investiert nur dann in einen Aktien-ETF für den Rest des Jahres, wenn der Januar höher schließt; andernfalls verbleibt sie in einem Cash-Proxy. Die Allokationsentscheidung wird einmal pro Jahr getroffen und bis zum Jahresende gehalten.

Am ersten Handelstag im Februar misst der Algorithmus die Gesamtrendite des Aktien-ETF im Januar. Ist die Rendite positiv und übersteigt der Orderwert den Mindestschwellenwert, kauft er den Aktien-ETF und hält ihn bis Dezember. War der Januar negativ, wird stattdessen der Cash-ETF gehalten. Der Prozess wiederholt sich jedes Jahr.

## Details

- **Einstiegskriterien**:
  - Am ersten Handelstag im Februar die gesamte Januarrendite des `EquityETF` berechnen.
  - `EquityETF` kaufen, wenn die Rendite positiv ist und die Ordergröße >= `MinTradeUsd`; andernfalls `CashETF` halten.
- **Long/Short**: Nur Long in Aktien oder Bargeld.
- **Ausstiegskriterien**: Die Aktienposition am letzten Handelstag des Jahres schließen.
- **Stops**: Keine.
- **Standardwerte**:
  - `EquityETF` – ETF, der den Aktienmarkt repräsentiert.
  - `CashETF` – Cash-Proxy-ETF.
  - `CandleType` = 1 Tag.
  - `MinTradeUsd` – Mindesttransaktionswert.
- **Filter**:
  - Kategorie: Saisonal.
  - Richtung: Nur Long.
  - Zeitrahmen: Langfristig.
  - Rebalancing: Jährlich.

