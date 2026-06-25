# Amstell Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Amstell Grid-Strategie ist ein C#-Port des MetaTrader 5 Expert Advisors `exp_Amstell.mq5`. Sie erstellt ein symmetrisches Kauf/Verkauf-Grid und wendet einen virtuellen Take Profit auf einzelne Einstiege an. Die Konvertierung folgt den StockSharp High-Level-API-Richtlinien und ersetzt die Tick-Verarbeitung durch Kerzenverarbeitung, während die ursprüngliche Idee intakt bleibt.

## So funktioniert es

1. **Initialisierung**
   - Die Strategie abonniert den konfigurierten Kerzentyp und startet den Positionsschutz.
   - Eine angepasste Pip-Größe wird aus dem `PriceStep` des Wertpapiers und der Dezimalpräzision berechnet. Fünfstellige und dreistellige Symbole erhalten automatisch einen 10-fachen Multiplikator, was der MT5-Implementierung entspricht.

2. **Erster Trade**
   - Wenn sowohl der letzte erfasste Kauf- als auch der Verkaufspreis leer sind (erster Start), wird sofort eine Markt-Kauforder gesendet. Dies startet das Grid genau wie der ursprüngliche Expert Advisor.

3. **Grid-Erweiterung**
   - Eine neue **Kauf**-Order wird ausgegeben, sobald der aktuelle Schlusskurs mindestens `StepPips` unter dem letzten erfassten Kaufpreis liegt.
   - Eine neue **Verkauf**-Order wird ausgegeben, sobald der Preis mindestens `StepPips` über dem letzten erfassten Verkaufspreis liegt.
   - Die Strategie verfolgt intern separate Long- und Short-Stapel, sodass abwechselnde Orders auch auf einem Netting-Konto koexistieren können. Entgegengesetzte Orders reduzieren zuerst den anderen Stapel, bevor neue Exposition hinzugefügt wird, was das Hedging-Verhalten der MT5-Version reproduziert.

4. **Virtueller Take Profit**
   - Jede offene Long-Position wird unabhängig überwacht. Wenn der Preis um `TakeProfitPips` steigt, wird eine Markt-Verkaufsorder nur für das Volumen dieser Position gesendet.
   - Jede offene Short-Position wird ähnlich in die entgegengesetzte Richtung behandelt. Der Take Profit ist "virtuell", weil Positionen programmatisch ohne Verwendung von broker-seitigen TP-Orders geschlossen werden.
   - Nachdem eine Richtung vollständig geschlossen wurde, während die entgegengesetzte Seite noch existiert, wird der entsprechende letzte Deal-Preis gelöscht, damit die nächste Order in dieser Richtung sofort auslösen kann, genau wie im ursprünglichen Code.

5. **Statusverfolgung**
   - Der `OnOwnTradeReceived`-Handler baut die Long/Short-Stapel aus ausgeführten Trades neu auf und ermöglicht einen eleganten Umgang mit Teilfüllungen und Reversierungen.
   - Letzte Kauf/Verkauf-Preise bleiben gecacht, wenn beide Seiten flach sind, sodass das Grid auf den erforderlichen Schritt wartet, bevor es nach einem vollständigen Reset wieder einsteigt.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `Volume` | `0.1` | Ordergröße für jede Marktorder in beide Richtungen. |
| `TakeProfitPips` | `50` | Abstand in Pips, der gewonnen werden muss, bevor eine einzelne Position geschlossen wird. |
| `StepPips` | `15` | Abstand in Pips zwischen aufeinanderfolgenden Grid-Orders in dieselbe Richtung. |
| `CandleType` | `1 Minute` | Kerzendatenquelle zur Annäherung der tick-basierten Logik. |

Alle pip-basierten Einstellungen berücksichtigen den Preisschritt und die Präzision des Instruments. Zum Beispiel entspricht bei EURUSD (5 Stellen) `StepPips = 15` einem Wert von 0,0015.

## Praktische Hinweise

- Die Strategie verwendet Kerzenschlusskurse, um die Tick-Level-Vergleiche aus dem MT5-Code zu emulieren. Für Hochfrequenzoperationen verringern Sie den Zeitrahmen.
- Standardmäßig gibt es keinen Stop-Loss. Wie bei jedem Grid-Ansatz können unkontrollierte Trends große Exposition akkumulieren. Verwenden Sie konservative Volumen und erwägen Sie sitzungsbasierte Überwachung.
- Da Take Profits virtuell verwaltet werden, werden geschlossene Trades sofort im PnL der Strategie widergespiegelt, ohne sichtbare TP-Orders beim Broker zu platzieren.
- Die Implementierung lässt gecachte letzte Preise unverändert, nachdem beide Seiten aufgelöst wurden. Dies bewahrt das ursprüngliche Verhalten, bei dem das Grid auf Preisverschiebung wartet, bevor es neu startet.

## Dateien

- `CS/AmstellGridStrategy.cs` – StockSharp-Strategie-Implementierung mit ausführlichen Inline-Kommentaren.
- `README.md`, `README_ru.md`, `README_zh.md` – Vollständige Dokumentation auf Englisch, Russisch und Chinesisch.

Dieser Port ist für weitere Anpassungen (z.B. Geldverwaltung, Risikolimits) direkt innerhalb des StockSharp-Ökosystems bereit.
