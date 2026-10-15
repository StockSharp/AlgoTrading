# Umgekehrte Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Reverse Strategy ist ein Mean-Reversion-Handelssystem, das Bollinger-Bänder und den Relative Strength Index (RSI) kombiniert, um erschöpfte Bewegungen zu identifizieren. Die Strategie sucht nach Preisumkehrungen in der Nähe der Bollinger-Umschläge und erfordert gleichzeitig, dass der RSI aus einer überverkauften oder überkauften Zone zurückkehrt. Sobald beide Bedingungen erfüllt sind, beginnt die Strategie gegen die vorherige Bewegung und verwaltet Geschäfte mit festen, bandbasierten Stopps und Zielen.

## Handelslogik

1. Abonnieren Sie die konfigurierte Kerzenserie (Standard-5-Minuten-Kerzen).
2. Berechnen Sie Bollinger-Bänder mithilfe eines einfachen gleitenden Durchschnitts mit der konfigurierten Periode und dem Abweichungsmultiplikator.
3. Berechnen Sie RSI mithilfe des konfigurierten Lookback-Zeitraums.
4. Verfolgen Sie die zuvor abgeschlossene Kerze, um Überkreuzungen zu erkennen:
   - **Long Setup**: Der vorherige Schlusskurs liegt unter dem vorherigen unteren Band und RSI liegt unter dem überverkauften Schwellenwert. Der aktuelle Schlusskurs muss wieder über das untere Band steigen, während RSI über das überverkaufte Niveau steigt.
   - **Short-Setup**: Der vorherige Schlusskurs liegt über dem vorherigen oberen Band und RSI liegt über dem überkauften Schwellenwert. Der aktuelle Schlusskurs muss wieder unter das obere Band fallen, während RSI unter das überkaufte Niveau fällt.
5. Wenn ein Long-Setup ausgelöst wird, kaufen Sie zum Marktwert, setzen Sie einen Schutzstopp eine Standardabweichung unter dem Einstiegsschluss und einen Take-Profit zwei Standardabweichungen darüber.
6. Wenn ein Short-Setup ausgelöst wird, verkaufen Sie zum Marktwert, setzen Sie einen Schutzstopp eine Standardabweichung über dem Einstiegsschluss und einen Take-Profit zwei Standardabweichungen darunter.
7. Offene Stellen verwalten:
   - Schließen Sie Long-Trades, wenn der Preis das obere Band berührt, den Stop erreicht oder das Take-Profit-Ziel erreicht.
   - Schließen Sie Short-Trades, wenn der Preis das untere Band berührt, den Stop erreicht oder das Take-Profit-Ziel erreicht.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Zeitrahmen für das Kerzenabonnement. | Zeitrahmen von 5 Minuten |
| `BollingerPeriod` | Anzahl der Balken, die für den gleitenden Durchschnitt Bollinger und die Standardabweichung verwendet werden. | 20 |
| `BollingerWidth` | Standardabweichungsmultiplikator, angewendet auf Bollinger Bänder. | 2,0 |
| `RsiPeriod` | Anzahl der Balken, die zur Berechnung des RSI verwendet werden. | 14 |
| `RsiOverbought` | RSI Schwellenwert, der überkaufte Bedingungen für Short-Einstiege signalisiert. | 70 |
| `RsiOversold` | RSI Schwellenwert signalisiert überverkaufte Bedingungen für lange Einträge. | 30 |

Alle Parameter unterstützen die Optimierung über den StockSharp Designer oder Runner. Durch Anpassen der Überverkauft-/Überkauft-Werte ändert sich, wie aggressiv die Umkehrerkennung ist, während die Bollinger-Breite steuert, wie weit sich der Preis ausdehnen muss, bevor Signale berücksichtigt werden.

## Nutzungshinweise

- Die Strategie verwendet das High-Level StockSharp API mit automatischen Kerzenabonnements und Indikatorbindung.
- Alle Handelsvorgänge basieren auf Marktaufträgen (`BuyMarket`/`SellMarket`). Stop-Loss- und Take-Profit-Level werden im Code und nicht als ausstehende Aufträge verarbeitet.
- Die Standardkonfiguration zielt auf größere Umkehrungen auf Intraday-Charts ab, kann jedoch durch Änderung von `CandleType` an längere Zeitrahmen angepasst werden.
- Erwägen Sie die Kombination der Strategie mit zusätzlichen Filtern (Trend, Volatilität, Sitzungszeit), wenn Sie sie in Live-Umgebungen ausführen.
