# Cross-Strategie (MQL 27596 Conversion)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Cross-Strategie** ist eine direkte Umsetzung des MetaTrader-Expertenberaters `Cross.mq4` (Repository-Eintrag `MQL/27596`). Beim ursprünglichen EA handelte es sich um einen einzelnen exponentiellen gleitenden Durchschnitt (EMA), der an den Eröffnungskursen der Balken gemessen wurde, und es wurden Take-Profit- und Stop-Loss-Levels mit festem Abstand angewendet. Dieser StockSharp-Port hält die Handelslogik intakt und nutzt gleichzeitig hochrangige API-Funktionen wie Kerzenabonnements, Indikatorbindung und verwaltete Positionsverfolgung.

## Handelslogik
1. **Indikator** – ein einzelner exponentieller gleitender Durchschnitt (EMA), der aus den Schlusskursen der Kerze berechnet wird. Der Zeitraum ist konfigurierbar und beträgt standardmäßig 200, entsprechend der Quelle MQL.
2. **Signalerkennung** – bei jeder fertigen Kerze vergleicht die Strategie die offene Kerze mit dem EMA-Wert:
   - Ein **bullisches Signal** tritt auf, wenn die Kerze über EMA öffnet, nachdem sie zuvor bei oder darunter geöffnet hatte. Dies reproduziert den `Cross(0, Open[0] > EMA)`-Aufruf im MQL-Skript.
   - Ein **bärisches Signal** tritt auf, wenn die Kerze unterhalb von EMA öffnet, nachdem sie zuvor bei oder darüber geöffnet hat (`Cross(1, Open[0] < EMA)` im Originalcode).
3. **Positionsmanagement** – wenn ein Signal ausgelöst wird, kehrt die Strategie die aktuelle Position vollständig um:
   - Wenn ein zinsbullisches Cross erscheint, während es flach oder short ist, kauft es genug Volumen, um das Short-Engagement abzudecken und eine neue Long-Position zu eröffnen.
   - Wenn ein bärischer Cross auftritt, während er flach oder long ist, wird genug Volumen verkauft, um das Long-Engagement abzuflachen und eine Short-Position aufzubauen.
4. **Risikokontrolle** – Nach dem Eingehen einer Position überwacht die Strategie die Höchst- und Tiefststände der Kerzen, um feste Take-Profit- und Stop-Loss-Ausstiege in Preisschritteinheiten zu implementieren. Diese Exits emulieren die `OrderSend`-Aufrufe, die sowohl `TakeProfit` als auch `StopLoss` in MetaTrader festlegen.

## Parameter
| Parameter | Standard | Beschreibung |
| --- | --- | --- |
| `EMA Length` | 200 | Zeitraum des EMA, der für die Kreuzerkennung verwendet wird. Muss größer als Null sein. |
| `Take Profit (steps)` | 200 | Abstand zum Take-Profit-Niveau, gemessen in Preisschritten. Auf Null setzen, um das Gewinnziel zu deaktivieren. |
| `Stop Loss (steps)` | 100 | Abstand zum Schutzstopp gemessen in Preisschritten. Auf Null setzen, um den Stopp zu deaktivieren. |
| `Candle Type` | Zeitrahmen von 1 Minute | Von der Strategie verarbeitete Kerzendatenquelle. Sie können zu anderen Zeitrahmen oder benutzerdefinierten Kerzentypen wechseln, die von StockSharp unterstützt werden. |

Das gehandelte Volumen wird durch die Eigenschaft `Volume` der Strategie gesteuert. Wenn ein Umkehrsignal eintrifft, sendet die Strategie `Volume + |Position|`, um sicherzustellen, dass das bestehende Engagement geschlossen wird, bevor die neue Position eröffnet wird.

## Ausführungsablauf
1. `OnStarted` abonniert die konfigurierte Kerzenserie und bindet den Indikator EMA mithilfe des High-Level-Helfers `Bind`.
2. Der Handler überspringt unvollendete Kerzen und wartet, bis EMA vollständig geformt ist. Sobald es fertig ist:
   - Verwaltet die aktive Position, indem es die Stop-Loss- und Take-Profit-Werte mit den Höchst-/Tiefstwerten der Kerze vergleicht.
   - Erkennt bullische und bärische Kreuze basierend auf dem Eröffnungspreis der Kerze im Verhältnis zu EMA.
   - Erteilt Marktaufträge, um die Position umzukehren, wenn ein neues Signal erscheint.
3. `OnNewMyTrade` verfolgt den durchschnittlichen Einstiegspreis und die Richtung der aktiven Position, sodass Ausstiegsprüfungen auch bei der Skalierung in Trades präzise Niveaus verwenden.
4. Es werden optionale Diagrammobjekte erstellt (sofern ein Diagramm verfügbar ist), um Kerzen, die EMA-Linie und ausgeführte Trades anzuzeigen.

## Details zum Risikomanagement
- **Stop-Loss** – berechnet als `entry price ± stop steps × price step` je nach Richtung. Die Strategie wird sofort beendet, wenn das Tief (Long) oder Hoch (Short) der Kerze das Stop-Level durchbricht.
- **Take Profit** – wird auf ähnliche Weise anhand der konfigurierten Gewinnschritte berechnet. Durch das Erreichen des Ziels wird die gesamte Position während der Kerze geschlossen, in der das Hoch/Tief den Schwellenwert überschreitet.
- **Kontoschutz** – `StartProtection()` wird einmal beim Start aufgerufen, sodass die Strategie alle globalen Schutzregeln berücksichtigt, die in StockSharp-Umgebungen konfiguriert sind.

## Anpassungstipps
- Kürzere Zeitrahmen oder Längen von EMA führen zu häufigeren Umkehrungen. Kombinieren Sie es mit größeren Stoppabständen, um Peitschenhiebe zu vermeiden.
- Um mehrere Symbole zu handeln, instanziieren Sie separate Strategieinstanzen mit ihren eigenen Wertpapier- und Kerzentypen.
- Halten Sie bei der Optimierung die Länge EMA und die Stop-/Take-Abstände innerhalb realistischer Grenzen für die Volatilität und Tickgröße des Instruments.

## Konvertierungshinweise
- Das MQL-Array `crossed[2]` ist zwei internen booleschen Flags zugeordnet, die über Kerzen hinweg bestehen bleiben.
- Die Funktion MQL `OrderSend` wird durch die Helfer `BuyMarket` und `SellMarket` von StockSharp dargestellt, wodurch sichergestellt wird, dass sowohl Umkehrungen als auch neue Einträge das ursprüngliche Verhalten widerspiegeln.
- EMA-Werte werden über den Bind-Callback bereitgestellt, wodurch direkte `GetValue`-Aufrufe gemäß den Repository-Richtlinien vermieden werden.

Indem Sie diese Details befolgen, können Sie die ursprüngliche MetaTrader-Strategie in StockSharp reproduzieren und dabei die volle Kontrolle über Datenquellen, Parameteroptimierung und Diagramme behalten.
