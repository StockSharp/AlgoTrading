# Macd-Secrets-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Macd-Secrets-Strategie** ist ein Multi-Timeframe-Momentum-Folgesystem, inspiriert vom ursprünglichen "Macd Secrets I" Expert Advisor für MetaTrader. Die StockSharp-Portierung nutzt die High-Level-API und konzentriert sich darauf, die MACD-Richtung über drei Zeitrahmen auszurichten, während Trades mit einer linear gewichteten gleitenden Durchschnittsbasis (LWMA) und einer Momentum-Abweichungsprüfung gefiltert werden. Die Strategie hält zu jedem Zeitpunkt nur eine einzelne Nettoposition und bietet damit ein vereinfachtes, transparentes Risikoprofil im Vergleich zum Quell-EA, der mehrere Orders pyramidisieren konnte.

## Signalerzeugung
### Long-Setup
1. Die schnelle LWMA liegt auf dem Ausführungszeitrahmen unter der langsamen LWMA, was signalisiert, dass der Preis nahe der unteren Seite des Trendkanals handelt (der ursprüngliche EA verwendet denselben Filter).
2. Die MACD-Linie liegt auf allen verfolgten Zeitrahmen über ihrer Signallinie: Ausführung, Trendbestätigung und monatliche Bestätigung. Dies spiegelt die dreifache MACD-Ausrichtung der MQL-Version.
3. Mindestens eine der letzten drei Momentum-Messungen auf dem Trendzeitrahmen weicht um das konfigurierte Minimum (Standard 0.3) von 100 ab. Die Abweichungsberechnung reproduziert die `MathAbs(100 - Momentum)`-Logik des EA.
4. Es ist keine Position offen.

Wenn die Bedingungen erfüllt sind, wird eine Markt-Kauforder mit dem konfigurierten Volumen platziert.

### Short-Setup
1. Die MACD-Linie liegt auf Ausführungs-, Trend- und Monatszeitrahmen unter ihrer Signallinie.
2. Mindestens eine der letzten drei Momentum-Abweichungen auf dem Trendzeitrahmen überschreitet den konfigurierten Short-Schwellenwert.
3. Es ist keine Position offen (die Portierung vermeidet Hedging und Skalierung).

Wenn alle Regeln gelten, wird eine Markt-Verkaufsorder gesendet.

### Trade-Management
- Die Strategie kann optional Schutzorders mit punktbasierten Distanzen für Stop-Loss und Take-Profit starten. Diese Distanzen werden mit dem Wertpapier-Preisschritt multipliziert, um Punkte in Preisinkremente umzuwandeln.
- Trailing-Stop-, Breakeven- oder equitybasierte Schutzlogik aus dem ursprünglichen EA ist nicht enthalten; StockSharp-Schutz wird einmal beim Start angewendet.
- Signale werden nur auf abgeschlossenen Kerzen bewertet, um Intrabar-Rauschen zu vermeiden.

## Multi-Timeframe-Daten
- **Primärer Zeitrahmen**: Ausführungsfrequenz (Standard 15 Minuten). MACD und das LWMA-Paar werden hier berechnet.
- **Trend-Zeitrahmen**: Bestätigung auf höherem Zeitrahmen (Standard 1 Stunde). Sowohl MACD als auch Momentum laufen auf dieser Subscription. Momentum-Abweichungen werden aus den letzten drei geschlossenen Kerzen gesammelt.
- **Monats-Zeitrahmen**: langfristige MACD-Bestätigung (Standard 30 Tage zur Annäherung an einen Kalendermonat).

Die Strategie überschreibt `GetWorkingSecurities`, sodass alle drei Subscriptions vom Connector von Anfang an angefordert werden.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ------------ | -------- |
| `OrderVolume` | Handelsvolumen in Lots. Muss positiv sein. | `0.1` |
| `TakeProfitPoints` | Take-Profit-Distanz in Punkten. Auf null setzen, um zu deaktivieren. | `50` |
| `StopLossPoints` | Stop-Loss-Distanz in Punkten. Auf null setzen, um zu deaktivieren. | `20` |
| `FastMaPeriod` | Länge der schnellen LWMA auf dem primären Zeitrahmen. | `6` |
| `SlowMaPeriod` | Länge der langsamen LWMA auf dem primären Zeitrahmen. | `85` |
| `MacdFastPeriod` | Schnelle EMA-Periode, die von jeder MACD-Instanz verwendet wird. | `12` |
| `MacdSlowPeriod` | Langsame EMA-Periode, die von jeder MACD-Instanz verwendet wird. | `26` |
| `MacdSignalPeriod` | Signal-EMA-Periode für MACD. | `9` |
| `MomentumPeriod` | Momentum-Rückblick auf dem Trendzeitrahmen. | `14` |
| `MomentumBuyThreshold` | Minimale absolute Abweichung von 100 für Long-Trades. | `0.3` |
| `MomentumSellThreshold` | Minimale absolute Abweichung von 100 für Short-Trades. | `0.3` |
| `PrimaryCandleType` | Kerzentyp für die Ausführung. Standard ist ein 15-Minuten-Zeitrahmen. | `15m` |
| `TrendCandleType` | Kerzentyp für Bestätigung. Standard ist ein 1-Stunden-Zeitrahmen. | `1h` |
| `MonthlyCandleType` | Kerzentyp für langfristige Bestätigung. Standard ist eine 30-Tage-Bar. | `30d` |

## Nutzungshinweise
- Der LWMA-Filter ist bewusst asymmetrisch: Nur Long-Trades verlangen, dass die schnelle LWMA unter der langsamen LWMA liegt, passend zum im MQL-Skript beobachteten Verhalten.
- Da die Portierung eine einzelne Nettoposition handelt, überspringt sie die martingaleartige Positionsgrößenlogik aus dem Quellcode (`LotsOptimized`). Wenn Stacking erforderlich ist, kann es wieder eingeführt werden, indem gefülltes Volumen verfolgt und mit `OrderVolume` verglichen wird.
- Stellen Sie sicher, dass der verbundene Broker oder die Datenquelle alle drei Kerzenzeitrahmen bereitstellen kann; andernfalls bleibt die Strategie inaktiv und wartet auf Indikatorbildung.
- Erwägen Sie, den Monatszeitrahmen für Märkte anzupassen, in denen 30-Tage-Kerzen nicht verfügbar sind, indem Sie einen benutzerdefinierten `DataType`-Parameter bereitstellen.
- Die Strategie arbeitet vollständig auf geschlossenen Kerzen und liest historische Indikatorbuffer nicht direkt, wodurch sie die StockSharp-Richtlinien zur Indikatornutzung einhält.

## Unterschiede zum ursprünglichen EA
- Trailing-Stop, Breakeven, geldbasierte Ausstiege und kontoweiter Equity-Schutz werden nicht portiert. Stattdessen wird StockSharp-Schutz mit statischen Distanzen verwendet.
- Order-Pyramiding und Martingale-Logik werden aus Gründen der Klarheit weggelassen. Die Positionsgröße bleibt konstant.
- Benachrichtigungen (Alerts, E-Mails, Push-Nachrichten) sind nicht implementiert.

## Haftungsausschluss
Algorithmischer Handel birgt erhebliche finanzielle Risiken. Testen Sie die Strategie mit historischen Daten und in einer simulierten Umgebung, bevor Sie sie mit echtem Kapital einsetzen.
