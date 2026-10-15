# Close-Profit-End-of-Week-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Close Profit End Of Week-Strategie** automatisiert das MetaTrader-Skript *Closeprofitendofweek.mq5*. Die Strategie überwacht das zugewiesene Instrument und verlässt freitags nach einer konfigurierbaren Cut-Off-Zeit jede profitable Position. Das Ziel besteht darin, schwebende Gewinne zu sichern, bevor das Wochenend-Gap-Risiko auftritt.

## Ursprüngliches MQL-Verhalten
Der Quell-Expert Advisor hat kontinuierlich Positionen über den Timer-Handler abgefragt. Immer wenn die Serverzeit mit Freitag und der konfigurierten Endzeit übereinstimmte, wurden alle offenen Positionen auf dem gehandelten Symbol durchlaufen. Jede Position mit positivem Gewinn wurde über eine Marktorder geschlossen. Kryptosymbole wurden ausdrücklich ausgeschlossen, da sie ohne Wochenendpausen gehandelt werden.

## StockSharp Implementierung
Der C#-Port behält die gleiche Schutzlogik bei, während er das High-Level-API von StockSharp verwendet:
- Abonniert nur eine konfigurierbare Kerzenserie, um regelmäßige Zeitaktualisierungen zu erhalten.
- Überprüft jede fertige Kerze und stellt sicher, dass es sich um einen Freitag handelt, dessen Schlusszeit nach dem benutzerdefinierten Cut-off liegt.
- Greift auf das verbundene Portfolio zu, um die Nettoposition für das Strategiesymbol auszuwerten.
- Erteilt für jedes noch offene profitable Engagement eine Marktorder in die entgegengesetzte Richtung.
- Überspringt die Routine vollständig, wenn das Instrument als Krypto-Asset klassifiziert ist.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `StartTradeTime` | Beginn des Überwachungsfensters (aus Gründen der Parität mit den MQL-Eingaben beibehalten). | `00:00` |
| `EndTradeTime` | Tageszeit am Freitag, nach der profitable Positionen geschlossen werden müssen. | `20:00` |
| `CloseTradesAtEndTime` | Aktiviert oder deaktiviert die automatische Schließroutine. | `true` |
| `CandleType` | Datenreihen zur Zeitverfolgung (standardmäßig 1-Minuten-Kerzen). | `TimeFrameCandle(1m)` |

## Ausführungsablauf
1. Beim Start der Strategie wird überprüft, ob das ausgewählte Wertpapier zur Krypto-Asset-Klasse gehört. Kryptoinstrumente werden ignoriert, um die Schutzklausel MetaTrader widerzuspiegeln.
2. Es wird ein Kerzenabonnement erstellt, um regelmäßige Rückrufe zu erhalten, sobald jede Kerze fertig ist.
3. Jede fertige Kerze löst die Zeitplanprüfungen aus. Lediglich Freitage, die nach der Annahmeschlusszeit geschlossen haben, führen zu einer weiteren Bearbeitung.
4. Die Strategie scannt das verbundene Portfolio, filtert die Position, die dem konfigurierten Wertpapier entspricht, und liest seinen variablen Gewinn ab.
5. Wenn der variable Gewinn größer als Null ist, wird eine Marktorder in die entgegengesetzte Richtung erteilt, um das Engagement vollständig zu schließen.
6. Doppelte Ausstiegsaufträge werden vermieden, indem aktive Aufträge überprüft werden, bevor neue Aufträge gesendet werden.

## Nutzungshinweise
- Verknüpfen Sie die Strategie mit einem Nicht-Krypto-Instrument zusammen mit demselben Portfolio, das die offenen Positionen besitzt, die Sie überwachen möchten.
- Die Strategie eröffnet keine neuen Trades; es verwaltet nur bestehende Positionen.
- Der Parameter `StartTradeTime` existiert für Konfigurationsparität und zukünftige Erweiterungen, wird jedoch von der aktuellen Logik nicht referenziert.
- Führen Sie für Portfolios mit mehreren Symbolen eine Instanz pro Instrument aus, um den Einzelsymbolbereich des MetaTrader-Skripts zu replizieren.

## Einschränkungen
- Die Gewinnerkennung basiert darauf, dass das Broker-Portfolio variable PnL meldet. Wenn das Portfolio nicht in Echtzeit aktualisiert wird, kann sich der Schließbefehl verzögern.
- Es werden nur Positionen für das konfigurierte Strategiesymbol geschlossen. Auf anderen Symbolen gehaltene Belichtungen bleiben davon unberührt.
- Die Prüfung wird bei Candle-Close-Ereignissen durchgeführt. Wählen Sie einen angemessen kurzen Zeitrahmen, wenn Sie einen engeren Zeitplan benötigen.
