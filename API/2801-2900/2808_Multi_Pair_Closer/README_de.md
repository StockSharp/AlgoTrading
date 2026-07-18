# Multi-Paar-Schließer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Multi-Paar-Schließer-Strategie** spiegelt das ursprüngliche MetaTrader-Skript wider, das einen Korb von Währungspaaren überwacht und alle offenen Positionen liquidiert, sobald der kombinierte schwebende Gewinn ein Ziel trifft oder der angesammelte Verlust einen Sicherheitsschwellenwert überschreitet. Die Konvertierung nutzt StockSharp's High-Level-API, um Gewinne zu verfolgen, eine Mindesthaltedauer durchzusetzen und Positionen über mehrere Wertpapiere in einer Aktion zu schließen.

## Logik

1. Die beobachteten Instrumente aus dem kommagetrennnten `WatchedSymbols`-Parameter auflösen. Wenn die Liste leer ist, wird das Haupt-`Security` verwendet.
2. Den ausgewählten Kerzentyp (Standard: 1-Minuten-Zeitrahmen) für jedes Instrument abonnieren. Jede fertige Kerze löst eine Gewinnauswertung aus.
3. Für jedes Instrument speichert die Strategie:
   - Den zuletzt berechneten Gewinn (`Positions[i].PnL`).
   - Den Timestamp, wann eine Position zuerst ungleich null wurde, um die `MinAgeSeconds`-Anforderung einzuhalten.
4. Nach jeder Aktualisierung wird der Nettogewinn über alle beobachteten Symbole berechnet:
   - Wenn `ProfitTarget` erreicht wird, werden alle Positionen, die älter als das Mindestalter sind, mit `BuyMarket` / `SellMarket`-Orders geglättet.
   - Wenn der Nettogewinn unter `-MaxLoss` fällt, wird dieselbe Liquidationslogik als Schutzstop angewendet.
5. Detaillierte Protokolle fassen den Gewinn pro Instrument und das aktuelle Korbergebnis nach jeder Auswertung zusammen.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `WatchedSymbols` | Kommagetrennte Liste von Wertpapieridentifikatoren zur Überwachung. Wenn leer, greift die Strategie auf das zugewiesene `Security` zurück. | `"GBPUSD,USDCAD,USDCHF,USDSEK"` |
| `ProfitTarget` | Nettogewinn (in Portfoliowährung), der erforderlich ist, um einen globalen Abschluss aller beobachteten Positionen auszulösen. | `60` |
| `MaxLoss` | Maximaler akzeptabler Verlust (in Portfoliowährung), bevor die Strategie den Korb zwangsschließt. | `60` |
| `Slippage` | Kompatibilitätsparameter, der den erlaubten Slippage aus dem ursprünglichen Skript widerspiegelt. Marktorders werden für Ausstiege verwendet, daher ist der Wert informativ. | `10` |
| `MinAgeSeconds` | Mindestlebensdauer einer Position, bevor die Strategie sie schließen darf. | `60` |
| `CandleType` | Kerzentyp, der für die periodische Überwachung verwendet wird (Standard: 1-Minuten-Kerzen). | `1 minute` |

## Hinweise

- Die Strategie verlässt sich auf `Positions[i].PnL`, bereitgestellt von StockSharp, um den schwebenden Gewinn zu messen. Sie ruft keine Handelshistorie ab und berechnet Preise nicht manuell.
- Positionen, die vor dem Start der Strategie geöffnet wurden, erben die Startzeit als ihren ersten gesehenen Timestamp. Sie werden erst geschlossen, nachdem das `MinAgeSeconds`-Intervall seit dem Strategiestart vergangen ist.
- Ausstiege werden mit Marktorders ausgeführt, um die Wahrscheinlichkeit sofortiger Liquidation zu maximieren. `Slippage` wird aus Gründen der Parität mit der MQL-Version protokolliert, wird aber nicht auf Preisberechnungen angewendet.
- Die Protokollausgabe repliziert das MetaTrader "Comment"-Fenster, indem der Gewinn jedes Symbols gefolgt vom Gesamtkorb-Total ausgegeben wird.

## Anforderungen

- Einen gültigen `SecurityProvider` zuweisen oder sicherstellen, dass die angeforderten Identifikatoren über den Connector verfügbar sind.
- Ausreichende Volumenkonfiguration pro Wertpapier bereitstellen, damit Marktorders die Position vollständig glätten können.
