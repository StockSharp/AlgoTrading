# CCIT3 Zero Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die CCIT3 Zero Cross-Strategie ist eine StockSharp-Portierung des MetaTrader 5-Expertenberaters, der Nulllinienumkehrungen des CCIT3-Oszillators handelt. Der Indikator wird durch Anwendung der Tillson T3-Glättungskette auf einen Commodity Channel Index (CCI) erstellt. Immer wenn der geglättete Oszillator das Vorzeichen wechselt, öffnet die Strategie entweder eine neue Position in der Umkehrrichtung oder schließt, sofern konfiguriert, die aktuelle Position und kehrt sie um.

## Handelslogik
- Berechnen Sie den CCI anhand des ausgewählten angewendeten Preises und Zeitraums.
- Glätten Sie den Oszillator mit einer Tillson T3-Pipeline. Es stehen zwei Berechnungsmodi zur Verfügung:
  - **Einfach** – dauerhafte sechsstufige Glättung, die sich wie der ursprüngliche neu berechnende MetaTrader-Indikator verhält.
  - **NoRecalc** – wertet das T3-Polynom nur für den aktuellsten Balken aus und erstellt die vereinfachte Version „ohne Neuberechnung“ aus dem Quellcode neu.
- Wenn der CCIT3-Wert von positiv auf negativ übergeht, eröffnen Sie eine Long-Position (oder kehren Sie eine Short-Position um, wenn `Trade Overturn` aktiviert ist).
- Wenn der CCIT3-Wert von negativ auf positiv übergeht, eröffnen Sie eine Short-Position (oder kehren Sie eine Long-Position um, wenn `Trade Overturn` aktiviert ist).
- Optionale Take-Profit-, Stop-Loss- und Trailing-Stop-Levels werden über den `StartProtection`-Helper von StockSharp verwaltet.

## Indikatoren und Berechnungen
- **Commodity Channel Index (CCI)** – läuft auf dem konfigurierbaren angewandten Preis (Schluss, Eröffnung, Hoch, Tief, Median, typisch, gewichtet) und Zeitraum.
- **Tillson T3-Glättung** – genau wie im MQL5-Indikator mit dem Volumenfaktor `B` implementiert. Der einfache Modus behält zustandsbehaftete EMA-Ketten über Balken hinweg bei, während NoRecalc das Polynom aus dem letzten rohen CCI-Wert neu berechnet.
- **Nulldurchgangserkennung** – Trades werden ausschließlich bei abgeschlossenen Kerzen ausgelöst und spiegeln die ursprünglichen Überprüfungen neuer Balken im Expert Advisor wider.

## Risiko- und Positionsmanagement
- `Take Profit (pts)` und `Stop Loss (pts)` werden mithilfe des `PriceStep` des Instruments in absolute Preisabstände umgewandelt.
- `Trailing Stop (pts)` aktiviert die nachlaufende Engine von StockSharp mit demselben Punktabstand.
- `Max Drawdown Target` skaliert das Basisauftragsvolumen anhand des aktuellen oder anfänglichen Portfoliowerts (`volume = OrderVolume * balance / target`) neu. Lassen Sie den Parameter auf Null, um eine feste Losgröße beizubehalten.
- `Trade Overturn` ermöglicht eine vollständige Umkehrung – zuerst wird die aktuelle Position geschlossen und dann eine neue in die entgegengesetzte Richtung eröffnet.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Volume` | 1 | Basis-Ordervolumen vor etwaiger Drawdown-Skalierung. |
| `Take Profit (pts)` | 1750 | Take-Profit-Distanz in Punkten. |
| `Stop Loss (pts)` | 0 | Stop-Loss-Distanz in Punkten. |
| `Trailing Stop (pts)` | 0 | Trailing-Stop-Distanz in Punkten (0 deaktiviert das Trailing). |
| `Trade Overturn` | falsch | Kehren Sie die Position bei entgegengesetzten CCIT3-Signalen um. |
| `CCI Period` | 285 | Lookback-Zeitraum für den Indikator CCI. |
| `CCI Price` | Typisch | Angewendeter Preis für die Fütterung von CCI. |
| `T3 Period` | 60 | Tillson T3 Glättungslänge. |
| `T3 Volume Factor` | 0,618 | Tillson T3 `B`-Koeffizient. |
| `Mode` | Einfach | CCIT3-Berechnungsmodus (`Simple` oder `NoRecalc`). |
| `Candle Type` | Zeitrahmen 1 Stunde | Für Kerzenabonnements verwendeter Zeitrahmen. |
| `Max Drawdown Target` | 0 | Balance-Divisor für die adaptive Volumengröße (0 deaktiviert die Skalierung). |

## Hinweise zur Implementierung
- Die Strategie abonniert eine durch `Candle Type` angegebene einzelne Kerzenquelle und verarbeitet nur abgeschlossene Kerzen.
- Alle Volumenwerte sind an der Volumenstufe des Wertpapiers ausgerichtet und durch `VolumeMin`/`VolumeMax` begrenzt.
- Die Standardparameter replizieren die veröffentlichte MT5-Konfiguration: CCIT3-Einfachmodus mit einem 285-Perioden-CCI, einer T3-Länge von 60 und einem Volumenfaktor von 0,618.
- Durch den Wechsel zu NoRecalc bleibt das Verhalten des ursprünglichen Indikators erhalten, der sofort auf das rohe CCI-Zeichen reagiert und gleichzeitig weiterhin positive/negative Signale erzeugt.
