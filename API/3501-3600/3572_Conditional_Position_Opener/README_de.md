# Strategie zur bedingten Positionseröffnung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Conditional Position Opener Strategy** reproduziert das Verhalten des ursprünglichen MetaTrader-Skripts *„Eine Kaufposition eröffnen, wenn keine offene Position vorhanden ist“*. Die Logik ist bewusst einfach: Wenn manuelle Schalter die Long- oder Short-Seite aktivieren, sendet die Strategie nur dann eine Marktorder, wenn in dieser Richtung kein offenes Exposure besteht. Dies verhindert doppelte Einträge und hält die Position an der ausgewählten Seite ausgerichtet.

Der StockSharp-Port sorgt dafür, dass der Implementierungsmakler neutral bleibt, indem er sich auf das High-Level-Candle-Abonnement und den integrierten Schutzhelfer des Frameworks verlässt. Stop-Loss- und Take-Profit-Abstände werden in Pip-Einheiten (Preisschritten) angegeben, sodass sie an jedes Instrument angepasst werden können.

## Strategielogik
1. Abonnieren Sie die konfigurierte Kerzenserie, um als Timing-Herzschlag zu fungieren.
2. Überprüfen Sie bei jeder fertigen Kerze die aktuelle Nettoposition.
3. Wenn der Long-Schalter aktiviert ist und die Position flach oder short ist, senden Sie eine Kauf-Market-Order.
4. Wenn der Short-Schalter aktiviert ist und die Position flach oder long ist, senden Sie eine Marktverkaufsorder.
5. Schutzaufträge werden automatisch über `StartProtection` verwaltet, das die Pip-Abstände in tatsächliche Preisversätze umwandelt.

Da StockSharp Nettopositionen verwendet, wird durch die gleichzeitige Aktivierung beider Seiten zuerst der Long-Trade und dann, wenn er nach der Füllung immer noch flach ist, der Short-Trade eröffnet. Dies spiegelt die Absicht des MQL-Codes wider, der mehrere Bestellungen pro Richtung vermeidet.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Volume` | `1` | Ordergröße für jeden Markteintritt. |
| `StopLossPips` | `100` | Stop-Loss-Distanz ausgedrückt in Preisschritten. Zum Deaktivieren auf Null setzen. |
| `TakeProfitPips` | `200` | Take-Profit-Distanz, ausgedrückt in Preisschritten. Zum Deaktivieren auf Null setzen. |
| `EnableBuy` | `false` | Bei `true` kann die Strategie Long-Positionen eröffnen, wenn kein Long-Engagement besteht. |
| `EnableSell` | `false` | Bei `true` kann die Strategie Short-Positionen eröffnen, wenn kein Short-Engagement besteht. |
| `CandleType` | `1 minute timeframe` | Kerzenreihe, die die periodische Auswertung vorantreibt. |

## Notizen
- Entfernungen werden mithilfe des `PriceStep` des Instruments in tatsächliche Preiserhöhungen umgerechnet. Wenn die Börse dies nicht meldet, wird der rohe Pip-Wert als absoluter Offset verwendet.
- `StartProtection` fügt nach jeder Ausführung automatisch Stop-Loss- und Take-Profit-Orders hinzu, sodass keine manuelle Orderverwaltung erforderlich ist.
- Die Strategie konzentriert sich auf die manuelle Auslösung und ist als Vorlage für die diskretionäre Ausführung über Parameter gedacht.
