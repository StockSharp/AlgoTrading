# Wss-Händler
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Portierung des Expertenberaters „Wss_trader“ MetaTrader 4, veröffentlicht auf forex-instruments.info. Das ursprüngliche EA kombiniert Umkehrniveaus im Camarilla-Stil mit klassischen Pivot-Abständen und eröffnet einen einzelnen Trade pro Balken, wenn der Preis während der Londoner Sitzung die konfigurierten Bänder durchbricht.

## Strategielogik

1. Zu Beginn jedes neuen Handelstages liest die Strategie das vorherige Tageshoch, -tief und -schluss aus, um eine Pivot-Leiter zu erstellen:
   - `Pivot = (High + Low + Close) / 3`
   - `Long entry = Pivot + Metric × point`
   - `Short entry = Pivot − Metric × point`
   - `Long stop = Short entry`
   - `Short stop = Long entry`
   - Ziele spiegeln die MetaTrader-Formeln `Close ± (High − Low) × 1.1 / 2` mit der gleichen Sicherheitsklammer wie der Originalcode wider.
2. Der Handel ist nur zwischen `Start Hour` und `End Hour` (einschließlich) zulässig. Außerhalb des Fensters wird jede offene Position sofort geschlossen.
3. Wenn eine fertige Kerze das Long-Einstiegsniveau überschreitet (Schlusskurs >= Niveau und vorheriger Schlusskurs < Niveau), kauft die Strategie einmal mit dem konfigurierten Volumen, fügt den vorberechneten Stopp und das Ziel hinzu und blockiert alle weiteren Einträge für diesen Balken. Für Shorts gilt eine Symmetrieregel.
4. Wenn sich die Position um mindestens `Trailing Points` Preisschritte positiv bewegt, wird der Stop nachgezogen, um den gleichen Abstand zum Schlusskurs beizubehalten. Der Anschlag bewegt sich nie nach hinten.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `Working Candle` | Primärer Kerzentyp, der für Intraday-Berechnungen verwendet wird. | `15 Minute` |
| `Daily Candle` | Kerzentyp, der zum Lesen des Vortages für Pivot-Levels verwendet wird. | `1 Day` |
| `Start Hour` | Stunde (0-23), wenn der Handel aktiviert ist. | `8` |
| `End Hour` | Stunde (0-23), in der der Handel keine neuen Eingaben mehr akzeptiert. | `16` |
| `Metric Points` | Abstand vom Pivot bis zu den Ausbruchsniveaus, gemessen in Preisschritten. | `20` |
| `Trailing Points` | Trailing-Stop-Distanz in Preisschritten. Auf `0` setzen, um das Nachstellen zu deaktivieren. | `20` |
| `Order Volume` | Bestellgröße, die den ursprünglichen `lots`-Parameter widerspiegelt. | `0.1` |

## Notizen

- Die Strategie schließt die aktuelle Position, sobald das Handelsfenster endet, und entspricht dabei dem Verhalten des ursprünglichen EA.
- Das Nachziehen wird an fertigen Kerzen verarbeitet. Das Intrabar-Trailing wird nicht reproduziert, da StockSharp Kerzenschließungen in diesem Port ausführt.
- Es ist nur ein Handel pro Kerze zulässig, wobei das Flag `tenb` aus der Version MQL repliziert wird.
