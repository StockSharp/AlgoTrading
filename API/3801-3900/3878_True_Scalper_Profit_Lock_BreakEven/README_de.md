# True Scalper Profit Lock BreakEven-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die True Scalper Profit Lock-Strategie ist eine Umsetzung des MetaTrader 4 Expert Advisors **TrueScalperProfitLock.mq4**. Es kombiniert einen kurzfristigen exponentiellen gleitenden Durchschnitts-Crossover mit RSI-basierten Polaritätsfiltern für Zeiteinträge. Die Strategie ist für Hochfrequenz-Scalping-Umgebungen konzipiert, in denen Positionen aktiv mithilfe eines Schutzstopps, eines festen Take-Profit-Niveaus und einer optionalen Break-Even-Sperre verwaltet werden.

## Handelslogik

- **Trendfilter:** Ein 3-Perioden-EMA, berechnet auf der vorherigen geschlossenen Kerze, muss über (für Käufe) oder unter (für Verkäufe) einem 7-Perioden-EMA aus demselben Balken handeln. Der Abstand zwischen den Durchschnittswerten muss mehr als eine Preisstufe betragen, um flache Marktbedingungen zu vermeiden.
- **RSI-Bestätigung:** Das Original EA bietet zwei Validierungsmodi. Methode A wartet darauf, dass der 2-Perioden-RSI den konfigurierten Schwellenwert zwischen den beiden zuletzt geschlossenen Kerzen überschreitet. Methode B prüft einfach, ob der RSI von vor zwei Kerzen über oder unter dem Schwellenwert liegt. Beide Modi können unabhängig voneinander oder zusammen verwendet werden, wobei Methode B standardmäßig aktiviert ist.
- **Order-Richtung:** Long-Trades erfordern, dass der schnelle EMA über dem langsamen EMA liegt, während der RSI überverkaufte Bedingungen anzeigt (`RSI < threshold`). Short-Trades spiegeln die Logik wider und erwarten überkaufte Werte.

## Positionsmanagement

- **Anfänglicher Schutz:** Beim Einstieg berechnet die Strategie einen Stop-Loss und Take-Profit mit fester Distanz anhand der Wertpapierpreisstufe. Beide Parameter folgen den Standardwerten der MetaTrader-Version (90 bzw. 44 Punkte).
- **Gewinnsperre:** Wenn diese Option aktiviert ist, wird der Stop-Loss auf die Gewinnschwelle zuzüglich eines konfigurierbaren Offsets verschoben, sobald der Preis um die Distanz `BreakEvenTriggerPoints` steigt. Dies spiegelt das „ProfitLock“-Verhalten des ursprünglichen EA wider.
- **Abbruch-Timer:** Zwei optionale Mechanismen schließen Trades nach einer vordefinierten Anzahl abgeschlossener Kerzen (`AbandonBars`). Methode A kehrt die Position sofort um, indem sie ein entgegengesetztes Einstiegsflag setzt, während Methode B einfach schließt und auf neue Indikatorsignale wartet.
- **Geldmanagement:** Die Lotgrößenformel entspricht dem Originalskript: Die Positionsgröße wird aus dem Portfoliosaldo, dem Risikoprozentsatz, dem Kontotyp (Mini vs. Standard) und den Live-Handelsgrenzen abgeleitet. Wenn Sie `UseMoneyManagement` auf `false` setzen, wird der Parameter für die feste Lautstärke wiederhergestellt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Zeitrahmen der verarbeiteten Kerzen. |
| `FixedVolume` | Basisauftragsvolumen, wenn die Geldverwaltung deaktiviert ist. |
| `TakeProfitPoints` / `StopLossPoints` | Gewinnziel und Schutzstopp in Preisschritten. |
| `UseRsiMethodA` / `UseRsiMethodB` | Aktivieren Sie RSI Bestätigungsmethoden, die mit EA übereinstimmen. |
| `RsiThreshold` | RSI-Ebene, die von beiden Bestätigungsmodi verwendet wird. |
| `AbandonMethodA` / `AbandonMethodB` | Aktivieren Sie die Varianten der Abbruchlogik. |
| `AbandonBars` | Anzahl der abgeschlossenen Kerzen, bevor die Abbruchlogik ausgelöst wird. |
| `UseMoneyManagement`, `RiskPercent`, `AccountIsMini`, `LiveTradingMode` | Steuerelemente zur Volumenberechnung. |
| `UseProfitLock`, `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` | Break-Even-Aktivierung und Offset. |
| `MaxOpenTrades` | Maximale Anzahl gleichzeitiger Trades (Standardverhalten ist eine offene Position). |

## Nutzungshinweise

1. Die Strategie wertet nur abgeschlossene Kerzen aus, um mit dem MetaTrader-Experten konsistent zu bleiben, der sich auf Balken-`shift`-Lookbacks verlässt.
2. Aktivieren oder deaktivieren Sie RSI-Methoden, um die genaue Konfiguration zu reproduzieren, die in der Originalvorlage verwendet wurde.
3. Break-Even- und Abbruchlogik basieren auf Kerzenhochs/-tiefs, um Preisschläge zu erkennen. Berücksichtigen Sie bei längeren Zeitrahmen die Möglichkeit von Überschreitungen innerhalb des Balkens.
4. Für die Geldverwaltung ist eine Portfolioverbindung erforderlich, die den `BeginValue` bereitstellt. Bei Nichtverfügbarkeit greift die Strategie auf das Festvolumen zurück.

## Dateien

- `CS/TrueScalperProfitLockBreakEvenStrategy.cs` – C#-Implementierung der Strategie.
- `README_zh.md` – Chinesische Dokumentation.
- `README_ru.md` – Russische Dokumentation.
