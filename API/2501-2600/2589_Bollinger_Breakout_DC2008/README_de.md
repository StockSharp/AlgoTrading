# Bollinger Ausbruch DC2008
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Neuimplementierung des MetaTrader Bollinger-Ausbruch-Expertenberaters von Sergey Pavlov (DC2008) für die StockSharp High-Level-Strategie-API. Die Strategie beobachtet abgeschlossene Kerzen, bewertet Bollinger-Bands-Ausbrüche an der ausgewählten Preisquelle und öffnet oder kehrt Positionen nur um, wenn der aktuelle Trade nicht verlustreich ist.

## Überblick
- Berechnet eine Bollinger-Bands-Hülle auf dem konfigurierten Zeitrahmen und dem angewandten Preis (Schluss, Eröffnung, Hoch, Tief, Median, Typisch, Gewichtet oder Durchschnitt).
- Generiert **Long**-Setups, wenn das Tief einer Kerze unter dem unteren Band schließt, während das Hoch unter dem mittleren Band bleibt (starke Abwärtsbewegung, die umkehren sollte).
- Generiert **Short**-Setups, wenn das Hoch einer Kerze das obere Band überschreitet, während das Tief über dem mittleren Band bleibt (starke Aufwärtsbewegung, die umkehren soll).
- Der ursprüngliche MQL-Experte handelte auf Ticks; in diesem Port werden Signale einmal pro fertiger Kerze verarbeitet, um Stabilität und Indikatorstimmigkeit zu gewährleisten.
- Positionen werden nur geöffnet oder umgekehrt, wenn die bestehende Position einen nicht negativen unrealisierten Gewinn zeigt, was den ursprünglichen Risikofilter repliziert.

## Handelsstrategie
### Indikator-Pipeline
1. Kerzen des gewählten `CandleType` abonnieren (Standard: 1-Stunden-Zeitrahmen).
2. Den ausgewählten angewandten Preis in einen Bollinger-Bands-Indikator einspeisen (`Length = BandsPeriod`, `Width = BandsDeviation`).
3. Kerzen ignorieren, bis der Indikator gültige obere, mittlere und untere Werte liefert.

### Einstiegskriterien
- **Kaufen**: `Low < LowerBand` **und** `High < MiddleBand`. Zeigt an, dass die gesamte Kerze unter der Mittellinie gehandelt wurde, nachdem sie das untere Band durchbrochen hat.
- **Verkaufen**: `High > UpperBand` **und** `Low > MiddleBand`. Zeigt an, dass die gesamte Kerze über der Mittellinie gehandelt wurde, nachdem sie das obere Band durchbrochen hat.

### Positionsfilter und -verwaltung
- Bei **keiner Position** öffnet die Strategie eine Marktorder mit dem konfigurierten `Volume`, wenn ein Signal erscheint.
- Wenn bereits eine Position existiert:
  - Bei einem Signal entgegen der aktuellen Richtung den unrealisierten Gewinn als `Position * (Close - PositionPrice)` mit dem Kerzenschlusskurs berechnen.
  - Wenn der unrealisierte Gewinn **negativ** ist, alle Aktionen für diese Kerze überspringen (identisch mit dem ursprünglichen Early-`return`).
  - Wenn der unrealisierte Gewinn **nicht negativ** und das Signal entgegengesetzt ist, eine umkehrende Marktorder der Größe `Volume + |Position|` senden, um sowohl die aktuelle Position zu schließen als auch eine neue in Signalrichtung zu etablieren.
  - Signale, die der aktuellen Richtung entsprechen, fügen der Position nichts hinzu (wie in der MQL-Version).
- Es gibt keine expliziten Stop-Loss- oder Take-Profit-Orders; Trade-Exits erfolgen nur über entgegengesetzte Signale, die den Gewinnfilter erfüllen.

## Parameter
| Name | Standardwert | Beschreibung |
| --- | --- | --- |
| `BandsPeriod` | 80 | Anzahl der Kerzen zur Berechnung des Bollinger-Gleitenden Durchschnitts und der Abweichungen. Muss positiv sein und ist für die Optimierung freigegeben. |
| `BandsDeviation` | 3.0 | Standardabweichungs-Multiplikator, der auf die Bollinger-Bands-Breite angewendet wird. Positiv, optimierbar. |
| `AppliedPrice` | Close | Preisquelle für den Indikator: Close, Open, High, Low, Median, Typical, Weighted oder Average (OHLC/4). Entspricht `ENUM_APPLIED_PRICE` aus MetaTrader. |
| `CandleType` | 1-Stunden-Zeitrahmen | Kerzentyp (Zeitrahmen) für Analyse und Orders. Kann auf jeden anderen von StockSharp unterstützten Datentyp umgestellt werden. |
| `Volume` (geerbt) | broker-abhängig | Ordergröße für neue Einstiege. Umkehrungen addieren automatisch die bestehende absolute Positionsgröße. |

## Unterschiede zum ursprünglichen MQL-Experten
- Der MetaTrader EA bewertete Bedingungen bei jedem Tick; dieser C#-Port wartet auf fertige Kerzen, um nicht auf unvollständige Daten zu reagieren.
- Der Indikatorversatz war im Quell-EA auf null fixiert und bleibt hier implizit; zusätzliche Versätze werden nicht exponiert.
- MetaTrader meldete den Floating-Profit direkt; der Port approximiert ihn über Kerzenschlusskurs und `PositionPrice`, was für den Vorzeichenvergleich des Filters ausreicht.
- Trade-Verwaltung, Stringnachrichten und Order-Kommentare aus der MQL-Version wurden weggelassen und konzentrieren sich ausschließlich auf die Signalgenerierung.

## Implementierungshinweise
- Kerzen, Indikatoren und Handelsaufrufe verlassen sich auf StockSharps High-Level-APIs (`SubscribeCandles().Bind(...)`, `BuyMarket`, `SellMarket`).
- Der Indikator wird automatisch gezeichnet, wenn in der UI ein Diagrammbereich verfügbar ist; Trades werden ebenfalls zum Debuggen dargestellt.
- Die Strategie setzt den Indikator bei jedem Start zurück und baut ihn neu auf, sodass Parameteränderungen beim nächsten Start sofort wirksam werden.
