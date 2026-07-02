# MO Bidir Hedge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **MO Bidir Hedge Strategy** ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `mo_bidir_v0_1`. Der ursprüngliche Roboter wurde für den Fünf-Minuten-Chart entwickelt und hielt immer ein abgesichertes Marktrisiko aufrecht: Jeder neue Balken eröffnete sowohl eine Long- als auch eine Short-Position mit vordefinierten Stop-Loss- und Take-Profit-Abständen. Die StockSharp-Version reproduziert dieses Verhalten mit fertigen Kerzen, High-Level-Order-Helfern und expliziten Risikoparametern, die in Instrumentenpunkten gemessen werden.

## Handelslogik
1. Abonnieren Sie den konfigurierten Kerzentyp (standardmäßig fünfminütiger Zeitrahmen) und verarbeiten Sie nur fertige Kerzen.
2. Sobald eine Kerze schließt, überprüfen Sie die inneren Heckenschenkel. Bleibt ein Teil offen, wartet die Strategie auf die Auslösung von Schutzaufträgen und eröffnet keine weiteren Positionen.
3. Wenn keine Beine aktiv sind, übermitteln Sie einen **Marktkauf**- und einen **Marktverkaufsauftrag** gleicher Größe. Jeder ausgeführte Auftrag wird zu einem unabhängigen Sicherungszweig, der von der Strategie verfolgt wird.
4. Nachdem jeder Eintrag gefüllt wurde, werden die Stop-Loss- und Take-Profit-Schwellenwerte berechnet, indem die konfigurierten Punktabstände mit der Preisstufe des Instruments (oder dem minimalen Preisinkrement, wenn die Stufe nicht verfügbar ist) multipliziert werden.
5. Bei jeder weiteren fertigen Kerze überprüft die Strategie die Kerzenhochs und -tiefs:
   - Lange Beine werden über einen Marktverkauf geschlossen, wenn das Tief das Stop-Level durchbricht; Wenn es nicht gestoppt wird, schließt ein Hoch, das das Ziel erreicht, das Segment mit Gewinn.
   - Kurze Beine schließen über einen Marktkauf, wenn das Hoch den Stopp berührt; andernfalls realisiert ein Tief, das das Ziel erreicht, den Gewinn.
   - Wenn beide Schwellenwerte innerhalb derselben Kerze liegen, wird der Stop-Loss priorisiert, da seine Berührung die Position in der MetaTrader-Implementierung zuerst geschlossen hätte.
6. Sobald alle Beine ihre Schutzniveaus erreicht haben, bereitet die Strategie beim nächsten Kerzenschluss sofort das nächste abgesicherte Paar vor.

Dieser Workflow behält die Parität mit der MT4-Logik bei und stützt sich dabei ausschließlich auf High-Level-StockSharp-APIs (`BuyMarket`/`SellMarket`) und eine kerzenbasierte Verarbeitung, die in den Konvertierungsrichtlinien vorgeschrieben ist.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Auftragsgröße gilt für beide Seiten der Absicherung. Muss positiv sein. |
| `StopLossPoints` | Abstand vom Einstiegspreis bis zum Schutzstopp, gemessen in Instrumentenpunkten. Verwenden Sie `0`, um den Stopp zu deaktivieren. |
| `TakeProfitPoints` | Zielabstand vom Einstiegspreis in Instrumentenpunkten. Verwenden Sie `0`, um das Gewinnziel zu deaktivieren. |
| `CandleType` | Zeitrahmen, der zum Erkennen neuer Balken verwendet wird. Standardmäßig ist ein Zeitrahmen von fünf Minuten eingestellt. |

Alle punktbasierten Entfernungen werden in absolute Preise umgerechnet, indem der konfigurierte Wert mit dem Instrument `PriceStep` multipliziert wird. Wenn der Schritt nicht definiert ist, wird der minimale Preisanstieg verwendet; Wenn keiner der beiden Werte verfügbar ist, bleiben die Schutzstufen inaktiv.

## Risikomanagement
- Beide Seiten der Absicherung nutzen das gleiche Fixvolumen und setzen auf symmetrische Schutzaufträge.
- Stop-Loss- und Take-Profit-Abstände spiegeln die MetaTrader-Standardwerte wider (80 bzw. 750 Punkte), wobei das Verhältnis „8 Pips vs. 75 Pips“ bei einem 5-stelligen Forex-Symbol erhalten bleibt.
- Jeder Zweig wird mit einer Marktorder geschlossen, wodurch die Marge sofort freigegeben wird und der verbleibende Zweig unbeaufsichtigt weitergeführt werden kann, bis sein eigenes Schutzniveau erreicht ist.

## Implementierungshinweise
- Die Strategie verarbeitet ausschließlich **fertige Kerzen**, um den projektweiten Konvertierungsregeln zu entsprechen. Intrabar-Stopp- oder Zielberührungen werden aus den Kerzenextremen abgeleitet, sodass Backtests ohne Tick-Daten davon ausgehen, dass der Stop vor dem Ziel ausgelöst wurde, wenn beide Preise innerhalb desselben Balkens erschienen.
- Das interne Hedge-Ledger verfolgt die Füllungen unabhängig von der Nettoportfolioposition. Dies spiegelt das Verhalten von MetaTrader wider, bei dem Long- und Short-Positionen gleichzeitig existieren.
- Es werden keine automatisierte Trailing-Logik, Sitzungsfilter oder zusätzliche Indikatoren eingeführt – die StockSharp-Version bleibt bewusst so minimalistisch wie der ursprüngliche Expert Advisor.

## Nutzungstipps
- Passen Sie `TradeVolume` an die Größe der Broker-Verträge an und stellen Sie sicher, dass das Instrument die gleichzeitige Kauf-/Verkaufsabsicherung unterstützt, wenn die Umgebung dies erfordert.
- Wenn Sie Pip-basierte Werte benötigen (z. B. 8 Pips), multiplizieren Sie diese mit der Anzahl der Punkte, die einen Pip für das aktuelle Symbol darstellen, bevor Sie den Parameter zuweisen.
- Kombinieren Sie die Strategie mit StockSharp Risikomodulen oder `StartProtection`, wenn zusätzliche Schutzmaßnahmen auf Portfolioebene erforderlich sind.
