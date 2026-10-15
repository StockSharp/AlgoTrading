# GG-RSI-CCI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den **GG-RSI-CCI** MetaTrader Expert Advisor mithilfe der StockSharp High-Level-API. Sie kombiniert die Indikatoren Relative Strength Index (RSI) und Commodity Channel Index (CCI), jeweils geglättet durch zwei gleitende Durchschnitte. Eine Position wird eröffnet, wenn beide Indikatoren in dieselbe Richtung zeigen.

## Logik

1. **Indikatoren**
   - RSI und CCI mit demselben Zeitraum berechnen.
   - Jeden Indikator mit einem schnellen und einem langsamen gleitenden Durchschnitt glätten.
2. **Signale**
   - **Kaufen** wenn der schnelle RSI über dem langsamen RSI liegt **und** der schnelle CCI über dem langsamen CCI liegt.
   - **Verkaufen** wenn der schnelle RSI unter dem langsamen RSI liegt **und** der schnelle CCI unter dem langsamen CCI liegt.
   - Wenn der Modus auf `Flat` gesetzt ist, schließt jeder neutrale Zustand die aktuelle Position.
3. **Risikomanagement**
   - Die Strategie ruft `StartProtection` einmalig beim Start auf. Stop-Loss- und Take-Profit-Niveaus können über den Risikomanager der Plattform konfiguriert werden.

## Parameter

| Name            | Beschreibung                                          |
|-----------------|-------------------------------------------------------|
| `CandleType`    | Zeitrahmen für die Berechnungen.                       |
| `Length`        | RSI- und CCI-Periode.                                 |
| `FastPeriod`    | Schnelle Glättungsperiode.                             |
| `SlowPeriod`    | Langsame Glättungsperiode.                             |
| `Volume`        | Auftragsvolumen.                                       |
| `AllowBuyOpen`  | Long-Positionen öffnen aktivieren.                     |
| `AllowSellOpen` | Short-Positionen öffnen aktivieren.                    |
| `AllowBuyClose` | Short-Positionen schließen aktivieren.                 |
| `AllowSellClose`| Long-Positionen schließen aktivieren.                  |
| `Mode`          | `Trend` schließt nur bei entgegengesetzten Signalen; `Flat` schließt auch bei neutralen Signalen. |

## Hinweise

Die Strategie verarbeitet nur abgeschlossene Kerzen und verwendet High-Level-Auftragshelfer (`BuyMarket` / `SellMarket`). Sie vermeidet direkten Zugriff auf Indikatorpuffer und speichert den Status intern.
