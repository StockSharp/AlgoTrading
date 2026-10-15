# Alexav D1 Profit GBPUSD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Tägliches Ausbruchssystem für GBPUSD, das einen auf Hochs berechneten EMA, RSI-Filter, MACD-Momentum-Bestätigung und ATR-basiertes Risikomanagement kombiniert. Das Skript reproduziert das Verhalten der vierstufigen Gewinnmitnahme und Gewinnsicherung der ursprünglichen MetaTrader-Version.

## Wichtige Fakten

- **Markt**: GBP/USD Spot oder CFD
- **Zeitrahmen**: Tageskerzen (konfigurierbar)
- **Richtung**: Long und Short
- **Positionsstil**: Multi-Ziel-Skalierung mit gemeinsamem Stop-Loss
- **Verwendete Instrumente**: EMA (High), RSI, MACD-Hauptlinie, ATR

## Indikatoreinrichtung

1. **EMA auf Hochpreisen** – Standardlänge 6, approximiert das dynamische Ausbruchsniveau.
2. **RSI** – Standardlänge 10, definiert Überkauft/Überverkauft-Korridore als Momentum-Filter.
3. **MACD-Hauptlinie** – schnell 5, langsam 21, Signal 14. Nur die Hauptlinie wird zur Messung der Momentum-Steigung verwendet.
4. **ATR** – Länge 28, liefert volatilitätsabhängige Stops und Ziele.

## Einstiegslogik

### Long-Einstiege

1. Der vorherige Tagesbalken öffnet unter dem EMA (High) und schließt darüber (Kreuzungsbestätigung aufwärts).
2. RSI bleibt zwischen **60** und **80** – verhindert Trades bei schwachem Momentum und vermeidet überdehnte Rallys.
3. Die MACD-Hauptlinie erfüllt eine von zwei Momentum-Prüfungen:
   - Der Wert vor zwei Balken ist negativ (was darauf hinweist, dass der Trend kürzlich positiv wurde), **oder**
   - Die relative Reduktion im absoluten MACD zwischen den letzten zwei Balken überschreitet den konfigurierbaren **MacdDiffBuy**-Schwellenwert (Standard 0.5).

Wenn alle Bedingungen zutreffen, werden vier gleiche Markt-Kaufaufträge platziert (Standard je 0.1 Lots). Jedes vorhandene Short-Engagement wird vor dem Senden des neuen Batches geflacht.

### Short-Einstiege

1. Der Balken öffnet über dem EMA (High) und schließt darunter.
2. RSI liegt zwischen **25** und **39** – spiegelt die Long-seitigen Schwellenwerte wider.
3. MACD vor zwei Balken ist positiv **oder** die relative Änderung im absoluten MACD zwischen den letzten zwei Balken liegt über **MacdDiffSell** (Standard 0.15).

Bei Bestätigung flacht die Strategie vorhandene Longs ab und sendet dann vier gleiche Marktverkäufe.

## Trade-Management

- **Anfangsstopp**: Gemeinsamer ATR-Stopp berechnet vom Eintrittskurs. Longs verwenden `entry - ATR * StopLossMultiplier` (Standard 1.6). Shorts verwenden `entry + ATR * StopLossMultiplier`.
- **Gewinnziele**: Vier inkrementelle ATR-basierte Niveaus pro Richtung: `1.0`, `1.5`, `2.0` und `2.5` ATR-Vielfache skaliert durch den `TakeProfitMultiplier`-Parameter (Standard 1). Jedes Niveau schließt ein Viertel der ursprünglichen Position über einen Marktauftrag, wenn der Preis das Niveau durchbricht.
- **Gewinnsicherungsverhalten**: Nach jedem Teilausstieg wird der Schutzstopp für die verbleibende Position auf den letzten Zielpreis verschoben. Dies imitiert den ursprünglichen EA, der Stop-Losses auf den ausgeführten Take-Profit-Preis modifiziert, wann immer ein TP-Deal auftritt.
- **Stopp-Behandlung**: Wenn der Preis das Schutzniveau intrabar berührt (unter Verwendung von Kerzen-Hoch/Tief), wird die verbleibende Position sofort zum Marktpreis geschlossen.

## Risikokontroll-Hinweise

- Die Strategie pyramidiert nicht über den Vier-Einstiegs-Batch hinaus. Ein neues Signal wird ignoriert, während Engagement in der gleichen Richtung verbleibt.
- ATR muss positiv sein; Signale werden übersprungen, wenn der Volatilitätsindikator noch nicht gebildet wurde.
- Parameteränderungen zur Laufzeit betreffen nur zukünftige Aufträge; das Volumen pro Auftrag wird beim Einstieg für korrektes Skalieren bei Ausstiegen erfasst.

## Parameter

| Name | Beschreibung | Standard |
|------|-------------|---------|
| `OrderVolume` | Volumen pro individuellem Marktauftrag im Batch | `0.1` |
| `EmaPeriod` | EMA-Länge angewendet auf Kerzen-Hochs | `6` |
| `RsiPeriod` | RSI-Durchschnittsperiode | `10` |
| `AtrPeriod` | ATR-Durchschnittsperiode | `28` |
| `StopLossMultiplier` | ATR-Vielfaches für den Schutzstopp | `1.6` |
| `TakeProfitMultiplier` | Basis-ATR-Vielfaches für Gewinnziele | `1.0` |
| `MacdFastPeriod` | MACD-Schnell-EMA-Länge | `5` |
| `MacdSlowPeriod` | MACD-Langsam-EMA-Länge | `21` |
| `MacdSignalPeriod` | MACD-Signal-EMA-Länge | `14` |
| `MacdDiffBuyThreshold` | Minimale MACD-Steigungsverbesserung für Long-Trades | `0.5` |
| `MacdDiffSellThreshold` | Minimale MACD-Steigungsverbesserung für Short-Trades | `0.15` |
| `RsiUpperLimit` | Maximaler RSI vor einem Long-Einstieg | `80` |
| `RsiUpperLevel` | Minimaler RSI für einen Long-Einstieg | `60` |
| `RsiLowerLevel` | Maximaler RSI für einen Short-Einstieg | `39` |
| `RsiLowerLimit` | Minimaler RSI vor Shorts | `25` |
| `CandleType` | Zeitrahmen für das Kerzenabonnement | `1 Day` |

## Einsatztipps

- RSI- und MACD-Schwellenwerte gemeinsam optimieren; das Lockern von RSI-Korridoren ohne Anpassung der MACD-Beschleunigungsfilter kann zu Fehlsignalen führen.
- Da Teilausstiege auf Kerzenextremen basieren, sind genaue Daten für Hoch/Tief-Werte für realistische Backtests wichtig.
- Immer mit ausreichend Kapital arbeiten, um vier gleichzeitige Aufträge pro Signal zu verwalten.
