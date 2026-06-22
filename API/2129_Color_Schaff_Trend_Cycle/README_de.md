# Color Schaff-Trendzyklusstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis des **Schaff Trend Cycle (STC)**-Indikators. Der STC wendet eine doppelte stochastische Berechnung auf eine MACD-Reihe an und oszilliert zwischen -100 und 100. Werte oberhalb des oberen Niveaus deuten auf bullischen Druck hin, Werte unterhalb des unteren Niveaus auf bärischen Druck.

## Handelslogik

- Abonnieren von Kerzen des ausgewählten Zeitrahmens.
- Berechnung des MACD mit schnellen und langsamen Exponentialglättungen.
- Anwendung von zwei aufeinanderfolgenden stochastischen Berechnungen zur Ableitung des STC.
- Wenn STC über das obere Niveau steigt und weiter aufwärts verläuft:
  - Schließen jeder Short-Position.
  - Eröffnen einer Long-Position.
- Wenn STC unter das untere Niveau fällt und weiter abwärts verläuft:
  - Schließen jeder Long-Position.
  - Eröffnen einer Short-Position.

Die Strategie agiert stets auf vollständig gebildeten Kerzen.

## Parameter

| Name | Beschreibung | Standard |
|------|--------------|----------|
| `FastPeriod` | Schnelle EMA-Periode für MACD | `23` |
| `SlowPeriod` | Langsame EMA-Periode für MACD | `50` |
| `Cycle` | Stochastische Zykluslänge | `10` |
| `HighLevel` | Überkauft-Schwelle für STC | `60` |
| `LowLevel` | Überverkauft-Schwelle für STC | `-60` |
| `CandleType` | Zeitrahmen der verarbeiteten Kerzen | `4h` |

## Hinweise

- STC-Werte werden auf einen Bereich von -100…100 skaliert, um den Vergleich mit den Standardniveaus zu erleichtern.
- Aufträge werden mit `BuyMarket()` und `SellMarket()` gesendet; Positionen werden automatisch umgekehrt, wenn entgegengesetzte Signale erscheinen.
- Diese Strategie konzentriert sich ausschließlich auf die Indikatorsignale und verwendet keine Stop-Loss- oder Take-Profit-Aufträge.
