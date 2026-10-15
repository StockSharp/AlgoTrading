# Sweet Spot Extreme-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sweet Spot Extreme ist eine direkte Portierung des MetaTrader 4 Expert Advisors „Sweet_Spot_Extreme.mq4“, der auf dem High-Level API von StockSharp basiert. Die Strategie sucht nach starken Pullbacks innerhalb eines bestehenden Trends, indem sie zwei exponentielle gleitende Durchschnitte auf 15-Minuten-Kerzen mit einem 30-Minuten-Commodity-Channel-Index-Filter (CCI) kombiniert. Die Positionsgröße spiegelt die ursprünglichen Risikokontrollen wider, einschließlich einer Lot-Reduzierung im MetaTrader-Stil nach Verluststrähnen.

## Kernlogik

1. **Bestätigung der Trendsteigung.** Der Haupt-EMA (`MaPeriod`, Standard 85) und der Schluss-EMA (`CloseMaPeriod`, Standard 70) werden mit 15-Minuten-Medianpreisen gefüttert. Bei einem langen Setup müssen beide EMAs nach oben geneigt sein. Bei einem kurzen Setup müssen beide nach unten geneigt sein.
2. **CCI-Erschöpfungsfilter.** Ein zweites Kerzenabonnement (standardmäßig 30 Minuten) versorgt den `CciPeriod` CCI mit Strom. Long-Trades werden nur ausgelöst, wenn CCI unter `BuyCciLevel` (−200) fällt, während Short-Trades CCI über `SellCciLevel` (+200) erfordern.
3. **Pyramidenlimit.** Die aggregierte Nettoposition darf `MaxTradesPerSymbol × volume` nicht überschreiten. Wenn ein neues Signal erscheint, schließt die Strategie alle entgegengesetzten Positionen und addiert dann die zulässige Kapazität in Signalrichtung auf.
4. **Ausgänge.** Positionen werden entweder geschlossen, wenn der Trend EMA seinen Steigungsvorteil verliert (was die MQL-Bedingung `MA <= MAprevious` widerspiegelt) oder nachdem sich der Preis um `StopPoints` Instrumentenpunkte zugunsten der Position bewegt hat.

## Risikomanagement

- **Risikobasiertes Volumen.** Die Bestellgröße beträgt standardmäßig `Portfolio.CurrentValue × MaximumRisk ÷ price`. Wenn Eigenkapitalinformationen fehlen, greift die Engine auf den Parameter `Lots` (oder die Strategie `Volume`) zurück.
- **Anpassung der Verluststrähne.** Nach zwei oder mehr aufeinanderfolgenden Verlustgeschäften wird die neue Ordergröße um `volume × losses ÷ DecreaseFactor` reduziert, entsprechend dem MQL-Helfer `LotsOptimized()`.
- **Normalisierung.** Das endgültige Volumen wird am `VolumeStep` des Instruments ausgerichtet, durch `MinVolume` begrenzt und durch `Security.MaxVolume` beschnitten, sofern angegeben.

## Parameter

| Name | Standard | Beschreibung |
|------|---------|-------------|
| `MaxTradesPerSymbol` | `3` | Maximal zulässige Anzahl aggregierter Einträge pro Richtung. |
| `Lots` | `1` | Fallback mit fester Losgröße, wenn Portfolio-Eigenkapital nicht verfügbar ist. |
| `MaximumRisk` | `0.05` | Anteil des Eigenkapitals, der für die Größe jedes neuen Handels verwendet wird. |
| `DecreaseFactor` | `6` | Teiler, der die nächste Order nach aufeinanderfolgenden Verlusten schrumpft. |
| `StopPoints` | `10` | Gewinnen Sie die Zielentfernung in Instrumentenpunkten. Zum Deaktivieren auf `0` setzen. |
| `MaPeriod` | `85` | EMA Zeitraum wird auf 15-Minuten-Kerzen für die Trendsteigungsprüfung angewendet. |
| `CloseMaPeriod` | `70` | EMA Zeitraum, der auf 15-Minuten-Kerzen für den Close-Glättungsfilter angewendet wird. |
| `CciPeriod` | `12` | Lookback, der für den 30-Minuten-CCI-Filter verwendet wird. |
| `BuyCciLevel` | `-200` | Für lange Einträge ist ein Überverkaufsschwellenwert von CCI erforderlich. |
| `SellCciLevel` | `200` | Für Short-Einstiege ist ein Überkaufschwellenwert von CCI erforderlich. |
| `MinVolume` | `0.1` | Nach der Normalisierung zulässiges Mindestvolumen. |
| `TrendCandleType` | `15m` | Kerzentyp, der für EMA-Berechnungen verwendet wird (Medianpreis). |
| `CciCandleType` | `30m` | Kerzentyp, der für den CCI-Filter verwendet wird. |

## Hinweise und Einschränkungen

- StockSharp arbeitet im Netting-Modus, sodass mehrere MT4-Tickets als eine einzige aggregierte Position dargestellt werden. Der `MaxTradesPerSymbol`-Guard begrenzt daher das Netto-Exposure, anstatt einzelne Bestellungen zu zählen.
- Das Original EA stützte sich bei der Größenbestimmung auf `AccountFreeMargin`. Dieser Port nähert sich dem an mit `Portfolio.CurrentValue`; Passen Sie `MaximumRisk` oder `Lots` an die Vertragsspezifikationen Ihres Maklers an.
- Stellen Sie sicher, dass beide Kerzenabonnements in der Datenquelle aktiviert sind. Andernfalls werden die Filter EMA oder CCI nie gebildet und die Strategie bleibt inaktiv.
