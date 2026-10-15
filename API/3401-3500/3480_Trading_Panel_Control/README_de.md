# Strategie zur Steuerung des Handelspanels
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Trading Panel Control Strategy** repliziert die Funktionalität des „Trading Panel“ MetaTrader 4-Dienstprogramms in StockSharp. Das ursprüngliche MQL-Panel ermöglichte es einem Händler, den aktiven Chart-Zeitrahmen zu wechseln und zwischen Instrumenten zu wechseln, indem er auf UI-Schaltflächen klickte. Die StockSharp-Version stellt dieselben Steuerelemente über Strategieparameter bereit, sodass die Hostanwendung (Designer, Terminal oder benutzerdefiniertes Dashboard) sie im Handumdrehen anpassen kann.

Im Gegensatz zum Quell-Expert Advisor sendet dieser Port keine Handelsaufträge. Sein Ziel besteht darin, das Diagrammabonnement mit dem aktuell ausgewählten Zeitrahmen und Instrument synchron zu halten und die letzten Kerzenschließungen zu protokollieren, um ein Feedback ähnlich den Textbeschriftungen im Originalpanel zu liefern.

## Schlüsselkonzepte

- **Dynamische Zeitrahmensteuerung** – wählen Sie zwischen M1, M5, M15, M30, H1, H4, D1 oder W1. Durch das Ändern des Parameters wird das Kerzenabonnement sofort neu erstellt.
- **Instrumentensuche** – Geben Sie eine Sicherheitskennung an, der gefolgt werden soll. Wenn diese Option aktiviert ist, durchsucht die Strategie das verbundene `ISecurityProvider`; Andernfalls wird auf die Sicherheit zurückgegriffen, die bereits mit der Strategie verbunden ist.
- **Kerzen-Feedback** – jede fertige Kerze wird mit ihrem Schlusskurs protokolliert, sodass der Betreiber die aktive Kombination aus Symbol und Zeitrahmen überprüfen kann.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `TimeFrameName` | Bevorzugter Zeitrahmencode (`M1`, `M5`, `M15`, `M30`, `H1`, `H4`, `D1`, `W1`). Standardmäßig ist `M15`. |
| `SecurityId` | Optionaler Bezeichner des zu steuernden Instruments. Lassen Sie das Feld leer, um die Eigenschaft `Security` der Strategie zu verwenden. |
| `AutoLookupSecurity` | Bei `true` löst die Strategie `SecurityId` bis `SecurityProvider` auf. Deaktivieren Sie es, um die bereits zugewiesene Sicherheit unverändert zu übernehmen. |
| `DefaultCandleType` | Fallback `DataType` wird verwendet, wenn ein unbekannter Zeitrahmen eingegeben wird. Standardmäßig werden Ein-Minuten-Kerzen verwendet. |

## Arbeitsablauf

1. **Start-up** – am `OnStarted` legt die Strategie das Zielwertpapier und den Zielzeitraum fest und beginnt dann ein Kerzenabonnement für diese Kombination.
2. **Laufzeitanpassungen** – Wenn Sie `TimeFrameName`, `SecurityId` oder `AutoLookupSecurity` ändern, während die Strategie ausgeführt wird, wird das Abonnement mit den neuen Einstellungen neu gestartet.
3. **Kerzenverarbeitung** – jede fertige Kerze aktualisiert die Eigenschaft `LastFinishedCandle` und schreibt einen Protokolleintrag mit der Sicherheitskennung, dem Zeitrahmencode und dem Schlusskurs.
4. **Herunterfahren** – Abonnements werden während `OnStopped` oder immer dann gestoppt, wenn die Strategie sie neu erstellen muss, weil sich Parameter geändert haben.

## Nutzungstipps

- Kombinieren Sie die Strategie mit einem Diagramm-Widget in StockSharp Designer, um den MT4-Panel-Workflow zu reproduzieren. Parametereditoren fungieren als Schaltflächen/Kombinationen.
- Lassen Sie `SecurityId` leer, wenn der Host der Strategieinstanz bereits ein `Security` zuweist.
- Die Protokollausgabe kann mit einem UI-Label oder einer Konsole verbunden werden, um die Informationslabels des Originalskripts zu imitieren.

## Unterschiede zur MQL-Version

- Keine grafischen Schaltflächen; Stattdessen werden Parameteränderungen verwendet.
- Es werden keine Handelsaktionen gesendet – die Logik beschränkt sich auf die Verwaltung und Protokollierung von Datenabonnements.
- Die Zeitrahmenliste ist mit der des Originalpanels identisch und sorgt so für ein vertrautes Verhalten für Händler, die von MT4 migrieren.
