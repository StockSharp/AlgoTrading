# DLM v1.4 Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung von Alejandro Galindos MetaTrader 4 Expert Advisor „DLM v1.4“. Der ursprüngliche Roboter kombiniert einen Fisher-Transform-Signalfilter mit einem Mittelungsschema im Martingal-Stil, das nach und nach ein Positionsraster aufbaut, wenn sich der Preis gegenüber dem letzten Eintrag bewegt. Die StockSharp-Version behält die gleichen Money-Management-Ideen bei, passt jedoch die Ausführungs- und Schutzlogik an die übergeordnete API an (Kerzenabonnements, Indikatorbindungen und Markt-/Limit-Helfer).

## Handelslogik
- Analysieren Sie fertige Kerzen aus dem konfigurierten Zeitrahmen und berechnen Sie zwei Indikatoren: die Fisher-Transformation und eine SMA-Glättung der Fisher-Werte.
- Bestimmen Sie die Korbrichtung aus der relativen Position der beiden Linien. Wenn Fisher über den Smoother steigt, bereitet sich die Strategie auf den Kauf vor; Wenn es unter den Glättewert fällt, bereitet es sich auf den Verkauf vor. Das Flag `ReverseSignals` kehrt diese Interpretation um.
- Eröffnen Sie sofort die erste Position (Market Order), sobald eine Richtung verfügbar und der automatische Handel aktiviert ist (`ManualTrading = false`).
- Während der Warenkorb aktiv ist, fügen Sie jedes Mal neue Einträge hinzu, wenn sich der Preis um `GridDistancePips` gegenüber der letzten Ausführung bewegt. Abhängig vom Flag `UseLimitOrders` werden die zusätzlichen Trades entweder als Marktaufträge (beim nächsten Kerzenschluss) oder als ruhende Limitaufträge gesendet, die genau einen Rasterschritt von der letzten Füllung entfernt positioniert sind.
- Das Volumen jedes neuen Handels folgt dem ursprünglichen Martingal-Wachstum: Multiplizieren Sie die Basislosgröße mit 1,5, wenn `MaxTrades > 12`, andernfalls verdoppeln Sie die Größe. Die Basisgröße selbst kann fest sein (`LotSize`) oder aus dem Kontoguthaben abgeleitet werden, wenn `UseMoneyManagement` aktiviert ist.
- Bei jeder Füllung werden die aggregierten Stop-Loss- und Take-Profit-Level aktualisiert, sodass der gesamte Korb einen einzigen Satz von Schutzniveaus aufweist. Die Trailing-Stop-Logik kann den Stop verschärfen, nachdem sich der Preis um `GridDistancePips + TrailingStopPips` in die profitable Richtung bewegt hat.

## Kontoschutz
- **Sicherer Gewinnschutz** (`SecureProfitProtection`): Sobald die Anzahl der offenen Einträge `OrdersToProtect` erreicht, wird der nicht realisierte Gewinn (in Kontowährung) mit `SecureProfit` verglichen. Bei Erreichen des Schwellenwerts wird der gesamte Warenkorb sofort geschlossen.
- **Aktienschutz** (`EquityProtection` + `EquityProtectionPercent`): überwacht den aktuellen Portfoliowert und schließt den Korb, wenn das Eigenkapital unter den ausgewählten Prozentsatz des zu Beginn der Strategie erfassten Eigenkapitals fällt.
- **Geldabzugsschutz** (`AccountMoneyProtection` + `AccountMoneyProtectionValue`): Stoppt den Handel, wenn der Währungsabzug vom anfänglichen Eigenkapital den konfigurierten Betrag übersteigt.
- **Lebenslanger Schutz** (`OrdersLifeSeconds`): erzwingt eine maximale Lebensdauer für den neuesten Eintrag; Bei Überschreitung des Limits werden alle Trades geschlossen und der Martingalzyklus gestoppt.
- **Freitagsfilter** (`TradeOnFriday`): Verhindert, dass neue Körbe freitags beginnen, wenn er deaktiviert ist.

Alle Schutzausgänge nutzen Marktaufträge, um die Ausführung zu gewährleisten. Ausstehende Limitaufträge werden storniert, wenn eine Schutzsperre ausgelöst wird oder wenn das Raster zurückgesetzt wird.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TakeProfitPips` | Auf jeden Eintrag wird eine gemeinsame Take-Profit-Distanz (Pips) angewendet. |
| `StopLossPips` | Anfängliche Stop-Loss-Distanz (Pips) für jeden neuen Trade. |
| `TrailingStopPips` | Trailing-Stop-Distanz, die nach der Triggerschwelle aktiv wird. |
| `MaxTrades` | Maximal zulässige Anzahl von Mittelungsschritten im Korb. |
| `GridDistancePips` | Minimale Gegenbewegung (Pips) vor dem Hinzufügen der nächsten Bestellung. |
| `LotSize` | Basislosgröße, wenn die Geldverwaltung deaktiviert ist. |
| `UseMoneyManagement` | Ermöglicht eine bilanzbasierte Größenbestimmung über die ursprüngliche Risikoformel. |
| `RiskPercent` | Risikoprozentsatz, der zur Ableitung der dynamischen Basislosgröße verwendet wird. |
| `AccountType` | Auf die dynamische Losgröße angewendete Skalierung (0 Standard, 1 Mini, 2 Mikro). |
| `SecureProfitProtection` | Aktiviert den Floating-Profit-Guard. |
| `SecureProfit` | Nicht realisierter Gewinn (Währungseinheiten), der zum Auslösen der Wache erforderlich ist. |
| `OrdersToProtect` | Mindestanzahl offener Einträge, bevor der sichere Gewinn aktiviert wird. |
| `EquityProtection` | Aktiviert das Eigenkapital-Sicherheitsnetz. |
| `EquityProtectionPercent` | Schwellenwert für den Eigenkapitalanteil im Verhältnis zum Beginn der Strategie. |
| `AccountMoneyProtection` | Ermöglicht einen auf Drawdown (Währung) basierenden Schutz. |
| `AccountMoneyProtectionValue` | Maximal tolerierter Drawdown in Kontowährung. |
| `TradeOnFriday` | Ermöglicht/verbietet das Öffnen neuer Körbe freitags. |
| `OrdersLifeSeconds` | Maximale Lebensdauer (Sekunden) für die letzte Bestellung vor der Liquidation. |
| `ReverseSignals` | Kehrt die Richtung der Fisher-Transformation um. |
| `UseLimitOrders` | Wechseln Sie zwischen Markt- und Limiteinträgen für Durchschnittsgeschäfte. |
| `ManualTrading` | Deaktiviert automatische Einträge, wenn auf „true“ gesetzt. |
| `CandleType` | Für die Indikatorberechnungen verwendeter Zeitrahmen. |
| `FisherLength` | Lookback-Länge für die Fisher-Transformation. |
| `SignalSmoothing` | SMA Zeitraum wird angewendet, um Fisher-Werte zu glätten. |
| `DefaultPipValue` | Fallback-Pip-Wert, der zur Umrechnung nicht realisierter Gewinne/Verluste in Währung verwendet wird. |

## Notizen
- Alle Kommentare im Quellcode sind gemäß den Repository-Richtlinien auf Englisch.
- Die Strategie basiert ausschließlich auf dem StockSharp-High-Level-API (`SubscribeCandles`, `Bind`, `BuyLimit`, `SellLimit` usw.) und manipuliert die Indikatorpuffer nicht direkt.
- Bei Money-Management-Berechnungen wird die ursprüngliche Risikoformel wiederverwendet, Volumen- und Preisanpassungen werden jedoch über `Security.ShrinkVolume` und `Security.ShrinkPrice` weitergeleitet, um die Vertragsspezifikation des Instruments zu berücksichtigen.
- Durch die Konvertierung bleibt das Verhalten des MetaTrader EA so nah wie möglich und berücksichtigt gleichzeitig StockSharp-Unterschiede (z. B. verwenden Warenkorbausstiege Marktaufträge, anstatt bestehende Aufträge zu ändern).
