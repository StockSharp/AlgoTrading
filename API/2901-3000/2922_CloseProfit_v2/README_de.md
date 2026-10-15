# CloseProfit V2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
CloseProfit V2 repliziert das Verhalten des ursprünglichen MetaTrader-Dienstprogramms, das alle aktiven Trading-Positionen zwangsweise schließt, sobald ein konfigurierbarer Gewinn- oder Verlustschwellenwert erreicht wird. Der StockSharp-Port fungiert als Kontoschutzmodul: Er überwacht den schwebenden PnL bei jeder abgeschlossenen Kerze und storniert, wenn Limits überschritten werden, ausstehende Orders und liquidiert Positionen. Die Strategie ist so konzipiert, dass sie neben diskretionären oder automatisierten Einstiegen läuft, die dasselbe Portfolio nutzen.

Im Gegensatz zu signalerzeugenden Systemen eröffnet CloseProfit V2 niemals selbstständig Positionen. Es beobachtet einfach Echtzeit-Gewinn- und Verlustmetriken und ermöglicht es Händlern, die in der MQL-Version verwendete "Panikknopf"-Logik zu automatisieren. Die Überwachungsfrequenz wird über eine Kerzenabonnierung gesteuert, was die Komponente sowohl mit historischen Backtests als auch mit Live-Trading-Umgebungen kompatibel macht.

## Funktionsweise
1. Wenn die Strategie startet, erfasst sie den aktuellen Portfoliowert als letzten Eigenkapital-Snapshot in Flachposition und startet die konfigurierte Kerzenabonnierung.
2. Jedes Mal, wenn eine Kerze abgeschlossen ist, speichert die Strategie den Schlusskurs und bewertet den schwebenden Gewinn:
   - Wenn `AllSymbols` deaktiviert ist, wird nur das primäre Instrument verfolgt. Der schwebende Gewinn wird als `Position * (lastClose - averagePrice)` berechnet, sodass nur unrealisierter PnL verwendet wird, was die MQL-Logik widerspiegelt, die offene Trades summiert.
   - Wenn `AllSymbols` aktiviert ist, vergleicht das Modul den aktuellen Portfoliowert mit dem letzten Flachposition-Eigenkapital-Snapshot. Dies misst den kombinierten unrealisierten Gewinn/Verlust über alle von der Strategie verwalteten Instrumente.
3. Wenn der schwebende Gewinn `ProfitClose` übersteigt oder unter `-LossClose` fällt, fordert die Strategie eine vollständige Liquidation an. Sie storniert sofort aktive Orders und sendet Marktanweisungen, um jedes betroffene Instrument flachzustellen.
4. Nachdem die Liquidation abgeschlossen ist und alle Positionen null erreichen, wird der Flachposition-Eigenkapital-Snapshot aktualisiert. Dies stellt sicher, dass die anschließende Überwachung vom neuen Kontostand beginnt und ein erneutes Auslösen durch realisierte Gewinne vermieden wird.

Die Implementierung spiegelt das Verhalten des ursprünglichen MQL-EA wider: Sie ignoriert historisch realisierten PnL und reagiert ausschließlich auf offene Positionen. Ein integrierter Schutzblock garantiert, dass die Schließroutine nur einmal pro Signal ausgeführt wird und nicht wiederholt Stornierungsanfragen sendet.

## Parameter
- **ProfitClose (Standard 10)** – Schwellenwert für schwebenden Gewinn in Kontowährung. Wenn unrealisierte Gewinne dieses Niveau erreichen, liquidiert die Strategie alle überwachten Positionen.
- **LossClose (Standard 1000)** – Schwellenwert für schwebenden Verlust. Sobald der unrealisierte Drawdown diesen absoluten Wert übersteigt, werden alle Positionen geschlossen, um weitere Verluste zu stoppen.
- **AllSymbols (Standard false)** – Wenn `false`, wird nur die der Strategie zugewiesene primäre `Security` beobachtet. Wenn `true`, aggregiert das Modul den schwebenden PnL für jedes Instrument im Positionsset der Strategie und liquidiert sie alle gleichzeitig.
- **CandleType (Standard 1-Minuten-Zeitrahmen)** – Kerzenreihe zur Bewertung. Der Schlusskurs der Kerze treibt Gewinnberechnungen an, wenn `AllSymbols` deaktiviert ist. Ein kürzerer Zeitrahmen bietet schnellere Reaktionen, während längere Frames die Rechenlast während Backtests reduzieren.

## Praktische Hinweise
- Starten Sie die Komponente zusammen mit anderen Trading-Strategien, die dasselbe Portfolio teilen. Sobald Schwellenwerte erreicht sind, wird CloseProfit V2 deren ausstehende Orders stornieren und ihre offenen Positionen schließen.
- Provisions- und Swap-Anpassungen sind in der StockSharp High-Level-API nicht verfügbar, daher basiert der schwebende PnL ausschließlich auf Preisunterschieden. Wenn diese Kosten relevant sind, erhöhen Sie die Schwellenwerte entsprechend.
- Da die Liquidation auf Marktorders angewiesen ist, stellen Sie sicher, dass ausreichend Liquidität oder Slippage-Puffer vorhanden sind, wenn Sie `ProfitClose` und `LossClose` konfigurieren.
- Die Kerzenabonnierung wird auch beim Backtesting verwendet, um deterministische Bewertungspunkte zu gewährleisten. Im Live-Trading können Sie zu schnelleren Frames wechseln, wenn Intrabar-Überwachung erforderlich ist.
- Die Strategie ruft beim Start `StartProtection()` auf, damit die integrierten Sicherheitsprüfungen von StockSharp (z. B. Wiederverbindungshandling) während des Betriebs des Dienstprogramms aktiv bleiben.

## Unterschiede zur ursprünglichen MQL-Implementierung
- MetaTraders "Magic Number"-Filter ist unnötig: StockSharp identifiziert Orders nach Strategie, sodass das Modul bereits die Positionen isoliert, die es kontrolliert. `AllSymbols` gilt daher für alle Instrumente, die von derselben Strategieinstanz verwaltet werden.
- Der MQL-EA verwaltete Chart-Labels zur Anzeige von Kontostand, Eigenkapital und Ticket-Zählungen. Die C#-Version verwendet Log-Nachrichten, da StockSharp-Charting optional und in automatisierten Ausführungen nicht immer verfügbar ist.
- Debug/Tester-Scaffolding, das in MQL automatisch Demo-Trades erstellte, wurde entfernt. Die StockSharp-Strategie konzentriert sich ausschließlich auf Überwachung und Liquidation.

## Einsatzbereich
Setzen Sie CloseProfit V2 ein, wann immer ein harter Stop auf schwebenden PnL benötigt wird—ob zum Schutz finanzierter Konten, zur Durchsetzung proprietärer Risikorichtlinien oder zur Automatisierung sitzungsbasierter Gewinnziele. Passen Sie den Kerzenzeitraum an, um ihn auf die von Ihrem Trading-Workflow geforderte Reaktionsgeschwindigkeit abzustimmen.
