# Fed-Modell-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses makroökonomische Timing-System vergleicht die Gewinnrendite des Aktienmarktes mit der Rendite 10-jähriger US-Staatsanleihen. Wenn Aktien eine höhere Rendite bieten, hält die Strategie einen Aktien-ETF; wenn Anleihen mehr abwerfen, wechselt sie zu Bargeld. Eine monatliche Regression auf den Renditeabstand prognostiziert den Wert des nächsten Monats, um rauschbedingte Wechsel zu reduzieren.

Am Monatsende prognostiziert der Algorithmus den Rendite-Spread des kommenden Monats anhand der Daten des letzten Jahres. Bei positivem Forecast kauft er Aktien, andernfalls hält er den Cash-Proxy. Positionen ändern sich nur, wenn die Prognose null kreuzt, was die Umschlaghäufigkeit minimiert.

## Details

- **Einstiegskriterien**:
  - Am Monatsende eine Regression über die letzten `RegressionMonths` Beobachtungen von `(EarningsYield - BondYield)` durchführen und den nächsten Wert prognostizieren.
  - Den Aktien-ETF kaufen, wenn der Forecast über null liegt und die Order `MinTradeUsd` erfüllt.
- **Long/Short**: Nur Long in Aktien oder Bargeld.
- **Ausstiegskriterien**: Aktienposition schließen, wenn der prognostizierte Rendite-Spread negativ wird.
- **Stops**: Keine.
- **Standardwerte**:
  - `Universe` – [Aktien-ETF, optionaler Cash-ETF].
  - `BondYieldSym` – Renditeserie 10-jähriger Staatsanleihen.
  - `EarningsYieldSym` – Gewinnrendite des Aktienmarktes.
  - `RegressionMonths` = 12.
  - `CandleType` = 1 Tag.
  - `MinTradeUsd` – Mindesttransaktionswert.
- **Filter**:
  - Kategorie: Makro.
  - Richtung: Nur Long.
  - Zeitrahmen: Monatlich.
  - Rebalancing: Monatlich.

