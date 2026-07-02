# No-Nonsense-Tester-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **No-Nonsense-Tester-Strategie** ist eine StockSharp-Portierung des MQL4-Expertenberaters „NoNonsenseTester“. Die Implementierung konzentriert sich auf den Kern-NNFX-Workflow, der eine Trendbasislinie validiert, auf zwei Bestätigungsindikatoren wartet, die Volatilität mithilfe von ATR überprüft und Trades mit strenger Exit-Logik überwacht. Die Strategie ist für das Experimentieren mit mehreren Parametern konzipiert und stellt daher alle wichtigen Schwellenwerte über `StrategyParam`-Objekte bereit, sodass sie innerhalb von StockSharp optimiert werden können.

## Handelslogik
1. **Basislinienfilter** – ein EMA mit konfigurierbarer Länge definiert die primäre Trendrichtung. Einträge werden nur berücksichtigt, wenn der Preis über der Basislinie schließt.
2. **Bestätigung Nr. 1** – ein RSI muss auf der bullischen (über dem Schwellenwert) oder bärischen (unter dem komplementären Schwellenwert) Seite liegen, um den Grundliniendurchbruch zu bestätigen.
3. **Bestätigung #2** – ein CCI muss mit dem Trend übereinstimmen und die konfigurierte absolute Größe überschreiten, um schwache Signale zu blockieren.
4. **Volatilitätsfilter** – ATR muss größer als der Wert `AtrMinimum` sein, um sicherzustellen, dass Trades nur dann getätigt werden, wenn der Markt eine ausreichende Spanne aufweist.
5. **Einstieg** – wenn die Basislinie kreuzt, die beiden Bestätigungen und der Volatilitätsfilter übereinstimmen, eröffnet die Strategie eine Position in der Richtung der Bewegung. Die Positionsgröße kann optional mit ATR über den Parameter `AtrEntryMultiplier` skaliert werden.
6. **Stopp und Ziel** – unmittelbar nach dem Einstieg berechnet die Strategie ATR-basierte Stop-Loss- und Take-Profit-Level. Das optionale ATR-Trailing aktualisiert den Schutzstopp ständig, während sich der Handel positiv entwickelt.
7. **Exit-Overlay** – ein zusätzlicher RSI mit kürzerer Periode überwacht offene Trades. Wenn es bei Long-Positionen das untere Band oder bei Short-Positionen das obere Band überschreitet, wird die Position geschlossen, auch wenn der Preis die Schutzniveaus nicht erreicht hat.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `BaselineLength` | Zeitraum der EMA-Basislinie. |
| `ConfirmationRsiLength` | Länge des Bestätigungsindikators RSI. |
| `ConfirmationRsiThreshold` | RSI-Level, das bullische und bärische Bestätigungen trennt. |
| `ConfirmationCciLength` | Länge des Bestätigungsindikators CCI. |
| `ConfirmationCciThreshold` | Minimale absolute CCI-Größe, um ein Signal zu akzeptieren. |
| `AtrPeriod` | ATR Lookback-Zeitraum. |
| `AtrEntryMultiplier` | Optionaler ATR-Multiplikator, der das gehandelte Volumen skaliert. |
| `AtrTakeProfitMultiplier` | ATR Multiplikator für das Take-Profit-Level. |
| `AtrStopLossMultiplier` | ATR Multiplikator für das Stop-Loss-Level. |
| `AtrTrailingMultiplier` | ATR-Multiplikator, der für dynamisches Trailing verwendet wird. Zum Deaktivieren auf `0` setzen. |
| `AtrMinimum` | Vor der Eröffnung von Trades ist ein Mindestwert von ATR erforderlich. |
| `ExitRsiLength` | Länge des Exit-Overlays RSI. |
| `ExitRsiUpperLevel` | RSI-Level, das kurze Exits erzwingt. |
| `ExitRsiLowerLevel` | RSI-Level, das lange Exits erzwingt. |
| `CandleType` | Für die Berechnungen verwendeter Kerzentyp (Zeitrahmen). |

## Diagrammobjekte
Die Strategie zeichnet automatisch:
- Quellkerzen.
- EMA Grundlinie.
- Markierungen für ausgeführte Trades.

## Optimierungshinweise
Jedes in der Logik verwendete `StrategyParam` stellt Optimierungsbereiche bereit, die die Flexibilität des ursprünglichen Testers widerspiegeln. Verwenden Sie die Optimierungstools von StockSharp, um Basislinienlängen, Bestätigungsschwellenwerte und Risikoeinstellungen zu durchsuchen und die von der Version MQL bereitgestellten Parameterrastertests zu reproduzieren.

## Nutzungstipps
- Kombinieren Sie die Strategie mit NNFX-Indikatorvoreinstellungen, indem Sie die Schwellenwerte an Ihre benutzerdefinierten Tools anpassen.
- Behalten Sie den Filter ATR im Auge. Ein `AtrMinimum` ungleich Null verhindert Trades während Sitzungen mit geringer Volatilität.
- Stellen Sie beim Testen von Continuation Trades `AtrTrailingMultiplier` größer als Null ein, um profitable Positionen atmen zu lassen und gleichzeitig Gewinne zu sichern.
