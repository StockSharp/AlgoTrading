# CDC PL RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **CDC PL RSI-Strategie** repliziert den MQL Expert Advisor *Expert_ADC_PL_RSI* innerhalb des StockSharp-Ökosystems. Das System durchsucht fertige Kerzen nach japanischen Candlestick-Umkehrmustern und bestätigt Eingaben mit dem Relative Strength Index (RSI). Long-Trades basieren auf dem *Piercing Line*-Muster bei überverkauften RSI-Bedingungen, während Short-Trades das *Dark Cloud Cover*-Muster in Kombination mit überkauften RSI-Werten erfordern. Der Ansatz hält das ursprüngliche Money-Management-Konzept einfach, indem er das Strategievolumen verwendet und StockSharp die Positionsgrößenbestimmung überlässt.

## Muster- und Indikatorlogik
- **Kerzenmuster**: Die Strategie rekonstruiert die MetaTrader-Logik durch die Analyse zweier zuletzt abgeschlossener Kerzen. Die Regeln für Piercing Line und Dark Cloud Cover spiegeln den ursprünglichen Code wider, einschließlich Prüfungen auf Lücken, lange Körper im Verhältnis zu einem adaptiven Körperdurchschnitt und die zugrunde liegende Trendrichtung.
- **RSI-Filter**: Ein 20-Perioden-RSI (optimierbar) bestätigt die Dynamik. Überverkaufte Werte (`RSI < 40`) schalten Long-Einträge frei, und überkaufte Werte (`RSI > 60`) schalten Short-Einträge frei. Der RSI-Verlauf wird auch verwendet, um Ausgänge zu erkennen, wenn der Oszillator die 30- oder 70-Ebenen in die entgegengesetzte Richtung überschreitet.
- **Körperdurchschnitt und Trendfilter**: Ein einfacher gleitender Durchschnitt der Kerzenkörpergrößen und weitere SMA der Schlusskurse replizieren die MetaTrader-Hilfsfunktionen (`AvgBody` und `CloseAvg`). Diese Durchschnittswerte verhindern Signale während des Rauschens und sorgen dafür, dass die Muster nach einer klaren Bewegung erscheinen.

## Handelsregeln
### Lange Einrichtung
1. Erkennen Sie ein Piercing-Line-Muster bei den letzten beiden abgeschlossenen Kerzen.
2. Erfordern, dass RSI von der vorherigen fertigen Kerze unter 40 liegt.
3. Wenn die Bedingungen bestehen, kaufen Sie zum Marktpreis. Wenn eine Gegenposition vorhanden ist, kehrt sich die Strategie um, indem die absolute Positionsgröße plus das konfigurierte Volumen gekauft wird.

### Kurze Einrichtung
1. Erkennen Sie bei den beiden letzten Kerzen ein Muster einer dunklen Wolkendecke.
2. Erfordern, dass RSI von der vorherigen fertigen Kerze über 60 liegt.
3. Wenn die Bedingungen bestehen, verkaufen Sie zum Marktpreis. Eine entgegengesetzte Position wird mit derselben Volumenlogik geschlossen und umgekehrt.

### Ausstiegsbedingungen
- Schließen Sie Long-Positionen, wenn RSI 70 nach unten oder 30 nach oben kreuzt, was signalisiert, dass die Dynamik nachgelassen hat oder umgekehrt ist.
- Schließen Sie Short-Positionen, wenn RSI 30 nach oben oder 70 nach unten überschreitet, was der MetaTrader-Implementierung entspricht.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `RsiPeriod` | 20 | RSI Lookback-Länge. Optimierbar zwischen 10 und 40 in 5er-Schritten. |
| `BodyAveragePeriod` | 14 | Zeitraum sowohl für die durchschnittliche Kerzenkörpergröße als auch für den Schlusspreistrendfilter. Optimierbar zwischen 10 und 30 in 5er-Schritten. |
| `CandleType` | 1-stündiger Zeitrahmen | Für Berechnungen verwendete Kerzenreihe. Jeder von StockSharp unterstützte Kerzentyp kann ausgewählt werden. |
| `Volume` (Basisklasse) | — | Handelsvolumen, das vor dem Start für die Strategieinstanz festgelegt wird. |

## Nutzung
1. Fügen Sie die Strategie einem Portfolio und einer Sicherheit in StockSharp Designer, Shell oder Runner hinzu.
2. Konfigurieren Sie Kerzentyp und -volumen entsprechend dem gehandelten Markt.
3. Passen Sie optional die Perioden RSI und Körperdurchschnitt an, um sie an die Volatilität des Instruments anzupassen, oder führen Sie Optimierungen mit dem StockSharp-Optimierer durch.
4. Starten Sie die Strategie und überwachen Sie die Diagrammüberlagerungen (Kerzen, RSI und Schlussdurchschnittslinie), um Musterbestätigungen und ausgeführte Trades zu überprüfen.

## Notizen
- Die Strategie ruft `StartProtection()` auf, sodass bei Bedarf integrierte Schutzroutinen konfiguriert werden können (Stop-Loss, Take-Profit, Trailing usw.).
- Es werden nur abgeschlossene Kerzen verarbeitet, wobei die Logik mit dem MQL Expert Advisor konsistent bleibt.
- Es werden keine zusätzlichen Sammlungen gespeichert; Indikatorinstanzen enthalten die für die Musterprüfungen erforderlichen Schiebefensterberechnungen.
