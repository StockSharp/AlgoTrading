# Ungerichtete RRS-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie portiert den MetaTrader 4 Expert Advisor „RRS Non-Directional“ auf das StockSharp-Framework. Das Original EA öffnet abgesicherte Kauf- und Verkaufskörbe abhängig vom gewählten Handelsmodus und verwaltet diese mit virtuellen Stop-Loss-, Take-Profit- und Trailing-Regeln. Die StockSharp-Implementierung reproduziert die konfigurierbaren Modi, die Geldrisikoabschaltung und die virtuelle Schutzlogik und passt gleichzeitig das Verhalten an die von StockSharp verwendeten Netting-Portfolios an. Hedge-basierte Modi wechseln daher zwischen Long- und Short-Engagements, anstatt gleichzeitig gegensätzliche Positionen einzuhalten.

## Handelslogik
- Abonnieren Sie Level-1-Daten, um die besten Geld-/Briefkurse zu lesen. Der von diesen Kursen gemeldete Spread wird vor jeder Einstiegsentscheidung mit `MaxSpreadPoints` verglichen.
- Markteinträge berücksichtigen den Parameter `TradingMode`:
  - `HedgeStyle` und `AutoSwap` spiegeln den doppelseitigen Modus wider, indem sie zwischen Long- und Short-Trades wechseln (StockSharp kann nicht gleichzeitig unabhängige Kauf- und Verkaufstickets halten).
  - `BuySellRandom` wirft bei jeder neuen Gelegenheit eine Münze.
  - `BuySell` öffnet immer die gegenüberliegende Seite der zuletzt geschlossenen Position.
  - `BuyOrder` und `SellOrder` beschränken den Handel auf eine einzige Richtung.
- Das externe `New_Trade` ist `AllowNewTrades` zugeordnet und bietet so eine schnelle Möglichkeit, alle neuen Marktaufträge zu pausieren.
- Jede Bestellung verwendet das konfigurierte `TradeVolume` und hängt das `TradeComment` an, um die Nachverfolgung auf der Brokerseite zu erleichtern.

## Risikomanagement und Exits
- Stop-Loss- und Take-Profit-Abstände werden in MetaTrader Punkten ausgedrückt. Sie werden mit dem Instrument `PriceStep` in Preiseinheiten umgerechnet, so dass die Logik maklerunabhängig bleibt.
- `StopMode`, `TakeMode` und `TrailingMode` wählen zwischen deaktivierter, virtueller und klassischer Verwaltung. Im Port StockSharp sind beide nicht deaktivierten Modi als virtuelle Prüfungen implementiert, die die Position über Marktaufträge schließen, wenn der Schwellenwert erreicht ist. Dadurch bleibt das Verhalten über alle Connectors hinweg deterministisch.
- Das Trailing-Management wird aktiviert, nachdem der Preis um `TrailingStartPoints` steigt, und behält dann einen dynamischen Stopp bei, der den besten Preis um `TrailingGapPoints` hinter sich lässt.
- Nicht realisierte Gewinne und Verluste werden bei jeder Aktualisierung der Stufe 1 neu berechnet. Wenn es unter den aus `RiskMode` und `MoneyInRisk` abgeleiteten Schwellenwert fällt, liquidiert die Strategie die Position sofort.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TradingMode` | Eintragsauswahl vom Original EA kopiert. Die Absicherungsmodi wechseln zwischen Long- und Short-Trades im Rahmen des Netting-Modells von StockSharp. |
| `AllowNewTrades` | Aktiviert oder deaktiviert neue Marktaufträge. |
| `TradeVolume` | Grundgröße für Bestellungen. |
| `StopMode` | Stop-Loss-Behandlung (`Disabled`, `Virtual`, `Classic`). |
| `StopLossPoints` | Stop-Loss-Distanz in MetaTrader Punkten. |
| `TakeMode` | Take-Profit-Abwicklung (`Disabled`, `Virtual`, `Classic`). |
| `TakeProfitPoints` | Take-Profit-Distanz in MetaTrader Punkten. |
| `TrailingMode` | Trailing-Stop-Verwaltung (`Disabled`, `Virtual`, `Classic`). |
| `TrailingStartPoints` | Erforderlicher Gewinn (Punkte) vor den hinteren Anschlagarmen. |
| `TrailingGapPoints` | Der Abstand (Punkte) wird hinter dem besten Preis beibehalten, sobald das Trailing aktiv ist. |
| `RiskMode` | Interpretiert `MoneyInRisk` entweder als Saldoprozentsatz oder als absoluten Währungsbetrag. |
| `MoneyInRisk` | Risikobetrag oder -prozentsatz, der eine vollständige Liquidation auslöst, wenn die variable Gewinn- und Verlustrechnung unter den Schwellenwert fällt. |
| `MaxSpreadPoints` | Maximal zulässiger Spread (Punkte) für neue Trades. |
| `SlippagePoints` | Die Einstellung für die Informationsschlupf wird beibehalten, um die Parität mit den ursprünglichen Eingaben zu gewährleisten. |
| `TradeComment` | Jeder Bestellung liegt ein Kommentar bei. |

## Hinweise und Einschränkungen
- AutoSwap basiert auf Swap-Kursinformationen in MetaTrader. StockSharp-Konnektoren stellen diese Zahlen normalerweise nicht über Level-1-Feeds bereit, daher greift der Modus auf `HedgeStyle` zurück und protokolliert die Herabstufung.
- Klassische Stop-Loss-, Take-Profit- und Trailing-Optionen werden virtuell ausgeführt. Broker, die native Schutzaufträge erfordern, sollten durch Strategieüberschreibungen auf niedrigerer Ebene gehandhabt werden.
- Da StockSharp Positionen pro Wertpapier aggregiert, wechselt die Strategie das Engagement in den Absicherungsmodi, anstatt zwei Tickets gleichzeitig zu behalten. Dieses Verhalten wird dokumentiert, damit Vorwärtstests den Erwartungen entsprechen.
