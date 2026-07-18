# Exp Amstell-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Exp Amstell Strategy** ist ein Grid-Trading-System, das aus dem ursprünglichen MetaTrader 4 Expert Advisor `exp_Amstell.mq4` umgewandelt wurde. Es platziert kontinuierlich Kauf- und Verkaufsaufträge auf dem Markt, wenn der Preis um eine konfigurierbare Anzahl von Punkten von der letzten Ausführung abweicht. Jeder einzelne Handel wird unabhängig verwaltet: Sobald sich der Markt um die angegebene Take-Profit-Distanz bewegt, sendet die Strategie einen Gegenauftrag, um den Gewinn für diese einzelne Ebene zu erfassen.

Im Gegensatz zu impulsgesteuerten Systemen bleibt Exp Amstell jederzeit aktiv. Es wartet nicht auf die Bestätigung von Indikatoren und sammelt stattdessen Positionen auf beiden Seiten des Buchs, während der Markt schwankt. Dieses Verhalten macht es sehr empfindlich gegenüber den gewählten Punktabständen und der Größe jeder Bestellung.

## Handelslogik
- **Tick-basierte Verarbeitung.** Die Strategie abonniert Kurse der Stufe 1 und reagiert auf jede Änderung des besten Geld- und Briefkurses, genau wie die Funktion `start()` im ursprünglichen MQL-Code.
- **Unabhängige Long- und Short-Stacks.** Kaufaufträge sind zulässig, wenn keine Long-Trades offen sind oder wenn der Briefkurs um mindestens die Wiedereintrittsdistanz vom letzten Long-Einstieg gefallen ist. Verkaufsaufträge nutzen die symmetrische Bedingung für den Geldkurs.
- **Take-Profit pro Trade.** Jede offene Ebene wird separat verfolgt. Wenn der Geldkurs (für Long-Positionen) oder der Briefkurs (für Short-Positionen) um die konfigurierten Take-Profit-Punkte steigt, schließt die Strategie nur diese Ebene mit einer Marktorder ab. Andere Schichten bleiben unberührt.
- **FIFO-Emulation.** Ausgeführte Geschäfte werden im FIFO aufgezeichnet, um die Ticket-basierte Abrechnung zu reproduzieren, die MetaTrader auf abgesicherte Positionen anwendet. Dadurch wird gewährleistet, dass Teilfüllungen zuerst die älteste ausstehende Schicht abbauen.
- **Netted Portfolio Awareness.** StockSharp verwaltet Nettopositionen. Wenn eine neue Kauforder eine offene Short-Position ausgleicht, entfernt die Strategie diese Short-Position aus ihrem synthetischen Stack, bevor der Rest als neue Long-Position erfasst wird.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `0.1` | Volumen jeder Marktorder, die eine neue Rasterschicht öffnet. |
| `TakeProfitPoints` | `int` | `30` | Entfernung in MetaTrader Punkten, die vom Preis abgedeckt werden muss, bevor eine einzelne Ebene geschlossen wird. |
| `ReentryDistancePoints` | `int` | `10` | Minimaler Punktabstand vom letzten Eintrag, bevor auf derselben Seite eine weitere Ordnung hinzugefügt wird. |

Die Strategie wandelt Punkte mithilfe des `PriceStep` des Instruments automatisch in tatsächliche Preisschritte um. Fünf- und dreistellige Anführungszeichen erhalten den MetaTrader-spezifischen Multiplikator, sodass `1 point` gleich `0.0001` ist (oder `0.01` für Symbole im JPY-Stil).

## Implementierungshinweise
- Daten der Stufe 1 sind ausreichend; Es ist kein Kerzenabonnement erforderlich. Die Strategie deklariert dies, indem sie `GetWorkingSecurities()` überschreibt und `(Security, DataType.Level1)` anfordert.
- `StartProtection()` wird während `OnStarted` aufgerufen, um sicherzustellen, dass der Läufer alle verbleibenden Positionen schließt, wenn die Strategie unerwartet stoppt.
- Alle Kommentare in der C#-Datei bleiben in Englisch und entsprechen den Projektrichtlinien.
- Da StockSharp saldierte Positionen verwendet, kann der Hafen gegensätzliche Käufe und Verkäufe nicht gleichzeitig offen halten. Wenn beide Seiten gleichzeitig handeln, verringert die neuere Order das bestehende Risiko, bevor eine neue Ebene entsteht.

## Nutzungstipps
1. **Kalibrieren Sie die Punktabstände.** Kleinere Abstände erzeugen dichtere Gitter, die in volatilen Märkten zu übermäßigem Handel führen können. Größere Abstände reduzieren die Aktivität, erhöhen aber den Absinken pro Schicht.
2. **Größe der Bestellungen mit Bedacht.** Grid-Systeme bauen schnell Risiko auf. Testen Sie konservative Volumina im Designer/Backtester, bevor Sie zum Live-Handel wechseln.
3. **Erwägen Sie manuelle Risikokontrollen.** Der ursprüngliche Experte hat keinen globalen Stop-Loss. Kombinieren Sie die Strategie mit Schutzmaßnahmen auf Portfolioebene, um das Extremrisiko zu begrenzen.
4. **Überwachen Sie die Ausführungsqualität.** Der Algorithmus geht davon aus, dass Marktaufträge in der Nähe des besten Geld-/Briefkurses ausgeführt werden. Slippage wirkt sich direkt auf die erzielten Take-Profit-Distanzen aus.

## Quelle
Konvertiert von `MQL/9027/exp_Amstell.mq4`.
