# AMA Trader 2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die AMA Trader 2-Strategie repliziert den Durchschnittsworkflow des ursprünglichen MetaTrader-Experten von Vladimir Karputov. Es kombiniert einen Kaufman Adaptive Moving Average (AMA)-Trendfilter mit einem Relative Strength Index (RSI)-Bestätigungsblock. Wenn der Preis über dem AMA schließt und der RSI in den überverkauften Bereich fällt, fügt die Strategie ein Long-Engagement hinzu; Die symmetrische Regel gilt für Short-Trades, wenn der Preis unter dem AMA schließt, während RSI einen überkauften Wert ausgibt. Durchschnittsgeschäfte werden in festen Losgrößen eingereicht und können durch Risikoparameter wie maximale Positionsanzahl, minimale Einstiegsabstände und schützende Trailing Stops eingeschränkt werden.

## Marktannahmen
- **Instrument**: Entwickelt für FX/CFD-Symbole, die mit engen Spreads gehandelt werden, aber anwendbar auf jedes liquide Instrument, bei dem eine Durchschnittsbildung akzeptabel ist.
- **Daten**: Arbeitet mit fertigen zeitbasierten Kerzen. Der Zeitrahmen ist über den Parameter `CandleType` konfigurierbar (Standard: 1 Minute).
- **Sitzungen**: Optionales Intraday-Fenster. Mit der Flagge `UseTimeWindow` kann der Handel auf eine Start-/Endzeit in UTC beschränkt werden.

## Indikatoren
1. **Kaufman Adaptive Moving Average (AMA)** – erkennt den vorherrschenden Trend mit konfigurierbaren schnellen/langsamen Glättungskonstanten und Mittelungslänge.
2. **Relative Strength Index (RSI)** – validiert Momentum-Extreme. Die Anzahl der aufeinanderfolgenden RSI-Messwerte, die ein Signal bestätigen müssen, wird durch `StepLength` gesteuert (0 verhält sich wie 1, entsprechend der MQL-Version).

## Handelslogik
1. Verarbeiten Sie nur fertige Kerzen und stellen Sie sicher, dass die Strategie online ist und zum Handel zugelassen ist.
2. Wenden Sie den optionalen Zeitfilter an; Überspringen Sie die Verarbeitung außerhalb des Intraday-Fensters, wenn diese Option aktiviert ist.
3. Aktualisieren Sie die Warteschlange der letzten RSI-Werte und berechnen Sie Trailing-Stop-Anpassungen für die vorhandene Belichtung.
4. **Langes Setup**: Schlusskurs über AMA und mindestens einer der überprüften RSI-Werte unter `RsiLevelDown`. Wenn die aktive Long-Position Geld verliert, wird vor der Standardeingabe eine Durchschnittsorder in die Warteschlange gestellt, die das „Verlustausgleichsverhalten“ des Expertenberaters nachahmt. Kurze Signale folgen der symmetrischen Regel (`RsiLevelUp`).
5. Einträge berücksichtigen `MaxPositions`, `MinStep` und `OnlyOnePosition`. Wenn `CloseOpposite` aktiviert ist, gleicht die Strategie zunächst die Gegenseite aus und berücksichtigt neue Einträge erst, nachdem der Flattening-Trade bestätigt wurde.
6. Jede neue Position kann mit festen Stop-Loss-/Take-Profit-Abständen verknüpft werden und optional einen gewinnbasierten Trailing-Stop mit Aktivierungs-, Abstands- und Schrittschwellenwerten ermöglichen.

## Risikomanagement
- **Feste Losgröße**: Alle Einträge verwenden `LotSize`, was eine Positionsgrößenbestimmung über den Parameter oder das Hosting-Portfolio ermöglicht.
- **Maximale Mittelungstiefe**: `MaxPositions` begrenzt, wie oft die Belichtung pro Richtung erhöht werden kann.
- **Abstandskontrolle**: `MinStep` erzwingt einen Mindestpreisabstand zwischen aufeinanderfolgenden Einträgen und reduziert so die Clusterbildung auf derselben Ebene.
- **Schutzausstiege**: Optionale Stop-Loss-, Take-Profit- und Trailing-Logik replizieren das Schutz-Toolkit des MetaTrader-Experten.
- **Gegenteiliges Risiko**: `CloseOpposite` zwingt die Strategie dazu, Short-Positionen zu schließen, bevor eine Long-Position eröffnet wird (und umgekehrt). `OnlyOnePosition` stellt sicher, dass die Strategie niemals beide Seiten gleichzeitig hält.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Kerzendatentyp/Zeitrahmen, der für Berechnungen verwendet wird. |
| `LotSize` | Volumen für jede Marktorder. |
| `RsiLength` | RSI Mittelungszeitraum. |
| `StepLength` | Anzahl der kürzlich überprüften RSI-Messwerte (0 → 1). |
| `RsiLevelUp` | RSI überkaufter Schwellenwert für Short-Einstiege. |
| `RsiLevelDown` | RSI überverkaufter Schwellenwert für lange Einträge. |
| `AmaLength` | AMA-Glättungslänge. |
| `AmaFastPeriod` | Schnelle Glättungskonstante für AMA. |
| `AmaSlowPeriod` | Langsame Glättungskonstante für AMA. |
| `StopLoss` | Fester Stoppabstand in Preiseinheiten (0 deaktiviert). |
| `TakeProfit` | Feste Zielentfernung in Preiseinheiten (0 deaktiviert). |
| `TrailingActivation` | Erforderlicher Gewinn, um den Trailing Stop zu aktivieren (0 deaktiviert). |
| `TrailingDistance` | Vom Trailing Stop aufrechterhaltener Abstand. |
| `TrailingStep` | Minimale Verbesserung, bevor der Trailing Stop verschärft wird. |
| `MaxPositions` | Maximale Mittelungseinträge pro Richtung (0 deaktiviert). |
| `MinStep` | Mindestabstand zwischen aufeinanderfolgenden Einträgen (0 deaktiviert). |
| `CloseOpposite` | Schließen Sie das entgegengesetzte Engagement, bevor Sie einen Handel eröffnen. |
| `OnlyOnePosition` | Blockieren Sie neue Einträge, sobald eine Position vorhanden ist. |
| `UseTimeWindow` | Aktivieren Sie die Intraday-Start-/Endzeitfilterung. |
| `StartTime` | Sitzungsstartzeit (UTC), wenn das Fenster aktiviert ist. |
| `EndTime` | Sitzungsendzeit (UTC), wenn das Fenster aktiviert ist. |

## Implementierungshinweise
- Nur API auf hoher Ebene: Kerzen werden über `SubscribeCandles` abonniert, AMA und RSI sind mit `.Bind` gebunden und alle Berechnungen erfolgen im gebundenen Rückruf ohne Verwendung verbotener Indikator-Getter.
- Die Positionsbuchhaltung spiegelt den MQL-Experten wider: Separate Akkumulatoren verfolgen Long- und Short-Volumina/Durchschnittspreise, um nicht realisierte PnL für Mittelungsentscheidungen zu bewerten.
- Trailing Stops konfigurieren den Stop-Loss-Abstand auf Strategieebene neu, anstatt Orderwarteschlangen direkt zu manipulieren, wodurch die Kompatibilität mit dem StockSharp-Ausführungsmodell gewahrt bleibt.
- Signale sind auf eine Ausführung pro Balken und Seite beschränkt, wodurch die MetaTrader-Prüfung reproduziert wird, die doppelte Einträge bei derselben Kerze verhindert.

## Unterschiede zum MetaTrader Expert
- MetaTrader-spezifische Parameter wie magische Zahlen, Abweichung, Freeze-Level-Prüfungen und Tester-Entzugsemulation werden weggelassen. Die StockSharp-Umgebung verwaltet Order Slippage und Gebühren intern.
- Stop-/Limit-Preise werden anhand des Kerzenschlusses und nicht anhand von Geld-/Brief-Ticks berechnet. Dies entspricht dem kerzenbasierten Workflow von StockSharp.
- Das Original EA verwendet Kontomargeneinstellungen, um dynamische Losgrößen zu berechnen. Der Port behält einen festen `LotSize` bei und überlässt die risikobasierte Dimensionierung der Hosting-Umgebung.
