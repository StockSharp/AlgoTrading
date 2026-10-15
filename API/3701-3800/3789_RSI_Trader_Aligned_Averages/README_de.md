# RSI Trader-Aligned-Averages-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
Diese Strategie reproduziert den „RSI-Händler“ MetaTrader Expert Advisor. Es richtet zwei Trendfilter – gleitende Preisdurchschnitte und geglättete RSI-Durchschnitte – so aus, dass sie in die Richtung des vorherrschenden Trends eintreten und austreten, wenn die Filter auseinanderlaufen (Seitwärtsregime). Der StockSharp-Port funktioniert auf jedem Instrument mit Kerzendatenunterstützung und verwendet standardmäßig stündliche Kerzen, wie in der Originalbeschreibung.

## Wie es funktioniert
1. Berechnen Sie RSI mit dem durch **RSI Zeitraum** angegebenen Zeitraum (Standard 14).
2. Glätten Sie den RSI-Stream mit zwei einfachen gleitenden Durchschnitten: einem kurzen (**Short RSI MA**) und einem langen (**Long RSI MA**).
3. Glatte Schlusskurse mit zwei gleitenden Durchschnitten: einem kurzen einfachen MA (**Short Price MA**) und einem langen linear gewichteten MA (**Long Price MA**).
4. Signale nur für fertige Kerzen generieren:
   - **Long** – beide Short-Durchschnitte (Preis und RSI) liegen über ihren Long-Gegenstücken.
   - **Short** – beide Short-Durchschnitte liegen unter ihren Long-Gegenstücken.
   - **Seitwärts** – die Durchschnittswerte stimmen nicht überein (einer zeigt einen Aufwärtstrend und der andere einen Abwärtstrend an). In diesem Fall wird jede offene Position geschlossen.
5. Bestellungen werden mit `BuyMarket` / `SellMarket` erteilt. Gegensätzliche Positionen werden abgeflacht, bevor eine neue Richtung eingeschlagen wird.

## Parameter
| Name | Beschreibung | Standard | Optimierbar |
| --- | --- | --- | --- |
| `RSI Period` | RSI Berechnungslänge. | 14 | Ja (7…28, Schritt 1) |
| `Short Price MA` | Länge des kurzen einfachen gleitenden Durchschnitts des Preises. | 9 | Ja (5…20, Schritt 1) |
| `Long Price MA` | Länge des langen linear gewichteten gleitenden Durchschnitts des Preises. | 45 | Ja (30…90, Schritt 5) |
| `Short RSI MA` | Länge des kurzen Glättungsdurchschnitts, der auf RSI angewendet wird. | 9 | Ja (5…20, Schritt 1) |
| `Long RSI MA` | Länge des langen Glättungsdurchschnitts, der auf RSI angewendet wird. | 45 | Ja (30…90, Schritt 5) |
| `Candle Type` | Für Kerzen verwendeter Datentyp. Standardmäßig ist der Zeitrahmen 1 Stunde. | H1 | Nein |

## Notizen
- Der Handel wird erst dann durchgeführt, wenn alle Indikatoren gebildet sind.
- Das Original EA verwendete Lots und Slippage-Einstellungen. StockSharp verwendet die Strategieeigenschaft `Volume` für die Ordergröße und überlässt die Ausführungs-Slippage-Verwaltung dem Handelsadapter.
- Es ist kein eingebauter Stop-Loss oder Take-Profit definiert; Ausgänge hängen von der seitlichen Erkennung ab. Zusätzliches Risikomanagement kann extern hinzugefügt werden.
- Diagramme zeichnen sowohl den Preis als auch RSI gleitende Durchschnitte, wenn der Diagrammdienst verfügbar ist.
