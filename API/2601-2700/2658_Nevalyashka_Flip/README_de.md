# Nevalyashka Flip-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein direkter StockSharp-Port des MetaTrader-Experten "Nevalyashka". Die Strategie wechselt immer zwischen Long- und Short-Trades: Sie beginnt mit einer Marktverkaufsorder, wartet darauf, dass die Position durch Stop Loss oder Take Profit geschlossen wird, und eröffnet dann sofort eine Marktorder in entgegengesetzter Richtung. Schutzorders werden für jeden Einstieg mit denselben Pip-basierten Abständen wie im Originalcode neu erstellt.

## Strategielogik

1. **Initialisierung**
   - Erkennt den Preisschritt des Instruments und die Dezimalstellen, um eine Pip-Größe identisch mit der MQL-Version zu berechnen (3/5-Dezimal-Paare werden mit 10 multipliziert).
   - Multipliziert das börsennotierte `MinVolume` mit dem Parameter `LotMultiplier`, um die Ordergröße zu erhalten, und rundet diese bei Bedarf auf den Volumenschritt.
2. **Kursbearbeitung**
   - Abonniert Auftragsbuch-Updates, um die neuesten besten Bid/Ask-Kurse zu erfassen und ahmt den `RefreshRates()`-Aufruf des Experten nach.
3. **Orderfluss**
   - Platziert eine anfängliche Marktverkaufsorder, sobald die besten Bid/Ask-Kurse verfügbar sind.
   - Nachdem eine Position geschlossen wurde, dreht es die Seite um (Kauf nach Verkauf, Verkauf nach Kauf) und gibt eine neue Marktorder mit demselben Volumen aus.
   - Für jeden ausgeführten Einstieg platziert die Strategie separate Stop-Loss- und Take-Profit-Orders mithilfe der Pip-Distanzparameter.

## Risikomanagement

- **Stop Loss**: Optional. Wenn `StopLossPips` größer als null ist, reicht die Strategie eine schützende Stop-Order ein (`SellStop` für Long-Positionen, `BuyStop` für Short-Positionen) bei `Einstieg ± StopLossPips * Pip`.
- **Take Profit**: Optional. Wenn `TakeProfitPips` größer als null ist, reicht die Strategie eine schützende Limit-Order ein (`SellLimit` für Long-Positionen, `BuyLimit` für Short-Positionen) bei `Einstieg ± TakeProfitPips * Pip`.
- Beide Schutzorders werden storniert, wenn die Position flach ist, um hängende Orders vor dem nächsten Flip zu vermeiden.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `LotMultiplier` | Multiplikator, der auf das minimale Instrumentvolumen angewendet wird. Das Ergebnis wird auf den Börsenvolumsschritt gerundet. | `1` |
| `StopLossPips` | Stop-Loss-Abstand in Pips. Auf `0` setzen, um den Stop zu deaktivieren. | `50` |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. Auf `0` setzen, um das Ziel zu deaktivieren. | `50` |

## Betriebshinweise

- Der Ansatz wechselt kontinuierlich das Exposure und eignet sich daher für Mean-Reversion-Märkte, bei denen ein abgeschlossener Kurs wahrscheinlich umkehrt.
- Funktioniert mit jedem Symbol, das Top-of-Book-Kurse bereitstellt; Pip-Berechnungen passen sich automatisch basierend auf der Kursgenauigkeit an.
- Die Slippage-Behandlung wird an die Börse delegiert—Orders werden ohne zusätzliche Prüfungen am Markt gesendet, genau wie beim ursprünglichen Experten.
- Die Strategie enthält keine Handelszeit-Filter, Nachrichten-Filter oder Trailing Stops. Solche Logik kann durch Erweiterung von `TryOpenNextPosition` oder `RegisterProtectionOrders` hinzugefügt werden.
