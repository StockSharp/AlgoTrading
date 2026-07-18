# Starter Triple Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader-Experten **Starter.mq5** auf die High-Level-API von StockSharp. Sie synchronisiert drei stochastische Oszillatoren (schnell, normal, langsam) mit passenden gleitenden Durchschnitten, die auf verschiedenen Zeitrahmen berechnet werden. Ein Trade wird nur zugelassen, wenn alle Filter dieselbe Richtung bestätigen und der Preis auf der richtigen Seite jedes verschobenen gleitenden Durchschnitts handelt.

## Handelslogik

1. Die Strategie abonniert drei Kerzenstreams:
   - **Schneller Zeitrahmen** (Standard `M5`).
   - **Normaler Zeitrahmen** (Standard `M30`).
   - **Langsamer Zeitrahmen** (Standard `H2`).
2. Für jeden Stream erstellt sie einen gleitenden Durchschnitt (konfigurierbarer Methode, Länge und angewendeter Preis) und einen stochastischen Oszillator mit denselben `%K`-, `%D`- und Verlangsamungsparametern.
3. Der langsame Zeitrahmen steuert die Ausführung. Wenn eine langsame Kerze schließt, werden die neuesten Werte aller Zeitrahmen verglichen:
   - Long-Setup: jede stochastische Linie hat `%K > %D`, alle `%K`-Werte liegen unter `50`, und der Preis liegt unter jedem verschobenen gleitenden Durchschnitt.
   - Short-Setup: jede stochastische Linie hat `%K < %D`, alle `%K`-Werte liegen über `50`, und der Preis liegt über jedem verschobenen gleitenden Durchschnitt.
4. Signale können optional durch `ReverseSignals` invertiert werden. Wenn ein Einstieg getätigt wird, kehrt die Strategie entweder die bestehende Exposition (wenn `CloseOppositePositions = true`) oder ignoriert das Signal, bis die entgegengesetzte Position geschlossen ist.
5. Nach einem Fill werden Stop-Loss- und Take-Profit-Niveaus im Preisraum simuliert. Ein Trailing Stop repliziert die originale MQL-Logik, indem `TrailingStopPips + TrailingStepPips` Gewinn erforderlich ist, bevor der Stop um `TrailingStopPips` verschoben wird.
6. Risikobasiertes Positions-Sizing spiegelt den MetaTrader `lot`/`risk`-Schalter wider. Wenn der Modus `RiskPercent` ist, wird das Trade-Volumen aus dem Kontowert, dem Risikoprozentsatz und dem Stop-Loss-Abstand in Pips berechnet.

## Parameter

| Name | Standard | Beschreibung |
|------|---------|-------------|
| `StopLossPips` | `45` | Schutz-Stop-Abstand in Pips. Auf `0` setzen, um den festen Stop zu deaktivieren. |
| `TakeProfitPips` | `105` | Take-Profit-Abstand in Pips. Auf `0` setzen, um das Ziel zu deaktivieren. |
| `TrailingStopPips` | `5` | Trailing-Stop-Offset nach dem Mindestfortschritt. |
| `TrailingStepPips` | `5` | Mindestgewinnfortschritt (in Pips), bevor der Trailing Stop sich bewegt. |
| `MoneyMode` | `RiskPercent` | Wählt zwischen festem Lot-Sizing und prozentualem Risiko pro Trade. |
| `MoneyValue` | `3` | Lot-Größe bei `FixedLot` oder Risikoprozentsatz bei `RiskPercent`. |
| `FastCandleType` | `M5` | Kerzentyp für den schnellen Indikatorensatz. |
| `NormalCandleType` | `M30` | Kerzentyp für den mittleren Indikatorensatz. |
| `SlowCandleType` | `H2` | Kerzentyp, der die Signalauswertung und Orders auslöst. |
| `MaPeriod` | `20` | Länge aller gleitenden Durchschnitte. |
| `MaShift` | `1` | Horizontale Verschiebung für jeden gleitenden Durchschnitt (Balken zurück). |
| `MaMethod` | `Simple` | Gleitender-Durchschnitt-Glättung: `Simple`, `Exponential`, `Smoothed` oder `Weighted`. |
| `MaPriceType` | `Close` | Angewendeter Preis für die gleitenden Durchschnitte. |
| `StochasticKPeriod` | `5` | `%K`-Länge für alle stochastischen Oszillatoren. |
| `StochasticDPeriod` | `3` | `%D`-Glättungslänge. |
| `StochasticSlowing` | `3` | Endgültiger Verlangsamungsfaktor für `%K`. |
| `ReverseSignals` | `false` | Tauscht die Long- und Short-Bedingungen aus. |
| `CloseOppositePositions` | `false` | Wenn `true`, kehrt die Position in einer einzigen Order um, wenn ein Signal in der entgegengesetzten Richtung erscheint. |

## Geldverwaltung

- `MoneyMode = FixedLot` sendet jede Order mit dem genauen `MoneyValue`-Volumen.
- `MoneyMode = RiskPercent` reproduziert das Originalverhalten: der riskierte Betrag entspricht `AccountValue * MoneyValue / 100`. Die Trade-Größe wird als `riskierter Betrag / (StopLossPips * Pip-Größe)` berechnet. Wenn `StopLossPips` null ist oder der Portfoliowert nicht verfügbar ist, verweigert die Strategie den Handel.

## Schutz und Trailing

- Stop-Loss- und Take-Profit-Niveaus werden intern verfolgt und mit Kerzenhochs/-tiefs verglichen, um MetaTrader's Schutzorders zu emulieren.
- Der Trailing Stop aktiviert sich erst, wenn der unrealisierte Gewinn `TrailingStopPips + TrailingStepPips` Pips überschreitet, was der originalen Anforderung entspricht, dass sowohl ein anfänglicher Offset als auch ein Mindestschritt erfüllt sein müssen, bevor der Stop verschoben wird.

## Multi-Zeitrahmen-Ausrichtung

Alle Indikatoren werden bei jeder geschlossenen Kerze ihres jeweiligen Zeitrahmens neu berechnet. Der langsame Zeitrahmen wartet, bis alle drei gleitenden Durchschnitte und Stochastiken geformt sind, und verwendet die neuesten verschobenen gleitenden Durchschnittswerte, was den MetaTrader-`iMA`-Shift-Parameter nachahmt. Dies stellt sicher, dass der StockSharp-Port Trades auf demselben Balken wie der originale MQL-Experte auslöst.
