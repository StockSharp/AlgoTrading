# Strategie Ausbruch der Vorgänger-Kerzen-Niveaus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert den MetaTrader-Expertenberater "Previous Candle Breakdown". Sie wartet darauf, dass der Preis über oder unter die vorherige Referenzkerze mit einem konfigurierbaren Einzug in Preisschritten ausbricht. Die Implementierung basiert auf StockSharp-High-Level-APIs mit Kerzenabonnements für Niveauberechnungen und Tick-Abonnements für Ausführungsentscheidungen.

## Handelslogik
1. Beim Schließen jeder Referenzkerze (standardmäßig 4 Stunden) speichert die Strategie das Hoch und Tief der vorherigen Kerze und verschiebt sie um `IndentSteps * Security.PriceStep`, um Ausbruchsniveaus zu erstellen.
2. Tick-Preise (letzte Trades) werden überwacht. Ein Long-Einstieg wird ausgelöst, wenn der Preis das obere Niveau erreicht, und ein Short-Einstieg, wenn der Preis durch das untere Niveau fällt.
3. Ein optionaler Filter für gleitende Durchschnitte erfordert, dass der schnelle MA (mit optionaler Vorwärtsverschiebung) für Long-Trades über dem langsamen MA und für Short-Trades darunter bleibt. Das Setzen eines MA-Zeitraums auf null deaktiviert den Filter.
4. Trades sind nur innerhalb des konfigurierten Sitzungsfensters zwischen `StartTime` und `EndTime` erlaubt. Mitternacht-übergreifende Sitzungen werden unterstützt.
5. Der schwebende Gewinn wird kontinuierlich überwacht: Stops, Ziele und Trailing-Regeln schließen bestehende Positionen, bevor ein Ausbruchssignal Umkehrungen auslösen kann.

## Risikomanagement
- **StopLossSteps / TakeProfitSteps** — Abstände in Preisschritten vom Einstiegspreis. Schritte werden über `distance = steps * Security.PriceStep` umgerechnet.
- **TrailingStopSteps / TrailingStepSteps** — aktiviert einen Trailing-Ausstieg, sobald die Position sich mindestens um den Trailing-Abstand zu Ihren Gunsten bewegt. Der Stop wird nur weiterbewegt, wenn der Gewinn um den Trailing-Schritt voranschreitet.
- **ProfitClose** — schließt alle Positionen, sobald der unrealisierte Gewinn (`Position * (letzter Preis - PositionPrice)`) den Schwellenwert überschreitet. Auf `0` setzen, um zu deaktivieren.
- **MaxNetPosition** — begrenzt die absolute Nettoposition, sodass die Strategie nicht über diesen Betrag hinaus pyramidisieren kann. Die Positionsgröße selbst wird durch die `Volume`-Eigenschaft der Strategie gesteuert.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Referenz-Zeitrahmen für die Berechnung der Ausbruchsniveaus. |
| `IndentSteps` | Verschiebung über/unter dem vorherigen Kerzenhoch/-tief in Preisschritten. |
| `FastMaPeriod` / `FastMaShift` | Schnelle MA-Länge und optionale Vorwärtsverschiebung (Bars). |
| `SlowMaPeriod` / `SlowMaShift` | Langsame MA-Länge und optionale Vorwärtsverschiebung (Bars). |
| `StopLossSteps` | Stop-Loss-Abstand in Preisschritten. |
| `TakeProfitSteps` | Take-Profit-Abstand in Preisschritten. |
| `TrailingStopSteps` | Trailing-Stop-Abstand (0 deaktiviert Trailing). |
| `TrailingStepSteps` | Mindestgewinn, der erforderlich ist, bevor der Trailing Stop vorrückt. Muss > 0 sein, wenn Trailing verwendet wird. |
| `ProfitClose` | Schwebender Gewinnziel, der alle Positionen schließt. |
| `MaxNetPosition` | Maximal erlaubte absolute Nettoposition. |
| `StartTime` / `EndTime` | Handelsfenstergrenzen. |

## Verwendungshinweise
- Setzen Sie die `Volume`-Eigenschaft der Strategieinstanz, um die Ordergröße zu steuern. Risikobasiertes Positionssizing aus der MetaTrader-Version ist absichtlich nicht portiert.
- Die gleitenden Durchschnitte verwenden einfache gleitende Durchschnitte (`SMA`). Wenn andere Glättungsmodi erforderlich sind, erweitern Sie die Strategie entsprechend.
- Der Gewinnschluss-Schwellenwert verwendet den unrealisierten Gewinn in Instrument-Preiseinheiten (Menge × Preisdifferenz). Passen Sie den Schwellenwert an Ihr Instrument an.
- Die Strategie arbeitet in einer Netting-Umgebung; umkehrende Trades senden Marktorders in der entgegengesetzten Richtung und schließen automatisch zuerst das aktuelle Exposure.
- Der Trailing Stop erfordert einen positiven `TrailingStepSteps`-Wert; andernfalls wirft die Strategie beim Start eine Ausnahme.

## Unterschiede zur originalen MQL-Version
- Geldmanagement basierend auf festen Lots oder Risikoprozentsatz ist nicht implementiert; StockSharp-Benutzer sollten die Größe über die `Volume`-Eigenschaft oder externe Portfolio-Manager verwalten.
- Nur einfache gleitende Durchschnitte werden unterstützt; das Original erlaubte verschiedene MA-Typen.
- Die Gewinnschluss-Logik verwendet den schwebenden PnL, der aus dem durchschnittlichen Positionspreis berechnet wird, anstatt der Kontowährung, da broker-spezifische Swap-/Provisionsdaten nicht direkt verfügbar sind.
- Das Logging wird von StockSharp behandelt; detaillierte Handelsergebnisnachrichten von MetaTrader werden weggelassen.
