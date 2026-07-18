# MAMy Expert Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Port des MetaTrader-5-"MAMy Expert"-Beraters von Victor Chebotariov auf die StockSharp-High-Level-Strategie-API.
- Reproduziert den ursprünglichen benutzerdefinierten Indikator, der drei gleitende Durchschnitte verschiedener Preisquellen vergleicht (Eröffnung, Schluss, gewichteter Preis).
- Arbeitet ausschließlich mit abgeschlossenen Kerzen und verwaltet höchstens eine Nettoposition gleichzeitig, was das Verhalten des MQL-Experten widerspiegelt.

## Indikatorbasis
- Die Strategie baut drei gleitende Durchschnitte mit derselben Länge und demselben Glättungsalgorithmus:
  - `MA(close)` – berechnet auf Kerzen-Schlusskursen.
  - `MA(open)` – berechnet auf Kerzen-Eröffnungskursen.
  - `MA(weighted)` – berechnet auf dem gewichteten Preis `(High + Low + 2 × Close) / 4`.
- Der `MaType`-Parameter wählt den Mittelungsalgorithmus (Simple, Exponential, Smoothed oder Weighted LWMA) für alle drei Serien aus und entspricht den `MODE_*`-Optionen von MetaTrader.
- Ein "Schlusspuffer" wird als Differenz `MA(close) − MA(weighted)` berechnet.
- Ein potenzieller "Eröffnungspuffer" wird nur erzeugt, wenn die gleitenden Durchschnitte sich in einer Trendkonfiguration ausrichten:
  - **Abwärts-Setup**: Sowohl `MA(close)` als auch `MA(weighted)` fallen, die Schluss-MA bleibt unter der gewichteten MA, beide bleiben unter der Eröffnungs-MA, und der Schlusspuffer nimmt ab.
  - **Aufwärts-Setup**: Sowohl `MA(close)` als auch `MA(weighted)` steigen, die Schluss-MA bleibt über der gewichteten MA, beide bleiben über der Eröffnungs-MA, und der Schlusspuffer nimmt zu.
  - Wenn eines der Setups zutrifft, wird der Eröffnungspuffer zu `(MA(weighted) − MA(open)) + (MA(close) − MA(weighted))`; andernfalls wird er auf null zurückgesetzt.
- Wenn ein frischer positiver Eröffnungspuffer von einem negativen Kreuz des Schlusspuffers begleitet wird, wird der Schlusspuffer auf null gezwungen, genau wie im ursprünglichen Indikatorcode.

## Signallogik
- **Einstiegsbedingungen**
  - **Kaufen** wenn der Eröffnungspuffer nach oben durch null kreuzt (`vorher ≤ 0`, `aktuell > 0`).
  - **Verkaufen** wenn der Eröffnungspuffer nach unten durch null kreuzt (`vorher ≥ 0`, `aktuell < 0`).
  - Einstiege werden nur berücksichtigt, wenn keine bestehende Position vorhanden ist.
- **Ausstiegsbedingungen**
  - **Long schließen** wenn der Schlusspuffer unter null kreuzt (`vorher ≥ 0`, `aktuell < 0`).
  - **Short schließen** wenn der Schlusspuffer über null kreuzt (`vorher ≤ 0`, `aktuell > 0`).
  - Ausstiege werden vor neuen Einstiegen ausgewertet, daher hält die Strategie niemals gleichzeitig Long- und Short-Engagements.
- Orders werden zu Markt mit dem konfigurierten `TradeVolume` ausgegeben. Schutzautomatisierung über `StartProtection()` spiegelt den Sicherheitsaufruf in den StockSharp-Beispielen wider.

## Charting und Datenfluss
- Abonniert den durch `CandleType` definierten Zeitrahmen und verarbeitet nur abgeschlossene Kerzen.
- Zeichnet Preiskerzen zusammen mit allen drei gleitenden Durchschnitten und annotiert ausgeführte Orders, und liefert dieselben visuellen Hinweise, die der ursprüngliche Indikator in MetaTrader bereitstellte.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Primärer Zeitrahmen, der Kerzen für den Indikator und Signale liefert. |
| `MaPeriod` | `int` | `3` | Länge für alle drei gleitenden Durchschnitte. |
| `MaType` | `MaCalculationType` | `Weighted` | Mittelungsalgorithmus (Simple, Exponential, Smoothed, Weighted). |
| `TradeVolume` | `decimal` | `1` | Volumen für jeden Marktorder-Einstieg. |

## Implementierungshinweise
- Verwendet den High-Level-`SubscribeCandles().Bind(...)`-Workflow und eingebaute Moving-Average-Indikatoren von StockSharp; es werden keine benutzerdefinierten Puffer jenseits der für die Signaldetection benötigten letzten Werte gespeichert.
- Signale werden erst ausgewertet, wenn alle Indikatoren vollständig gebildet sind und die Strategie bereit für den Live-Handel ist (`IsFormedAndOnlineAndAllowTrading()`).
- Die Strategie ignoriert absichtlich gleichzeitige Einstiege, solange eine Position offen ist, und spiegelt dadurch die Logik des Quell-Experten-Beraters getreu wider.
