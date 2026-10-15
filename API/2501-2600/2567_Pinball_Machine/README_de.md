# Pinball Machine-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Pinball Machine-Strategie** ist eine spielerische Konvertierung des MetaTrader-5-Expertenberaters "Pinball machine (barabashkakvn's edition)". Anstatt die Marktstruktur zu analysieren, emuliert die Strategie eine Lottermaschine: Jede fertiggestellte Kerze löst mehrere Zufallsziehungen aus, die zu einem Trade führen können, wenn zwei Zahlen übereinstimmen. Der StockSharp-Port bewahrt den Geist des ursprünglichen Experten und passt gleichzeitig Geldverwaltung und Ausführung an die High-Level-API an.

## Handelslogik
1. **Auslöser** – die Strategie arbeitet auf dem durch `Candle Type` definierten Zeitrahmen. Wenn eine Kerze abgeschlossen ist, läuft der Zufallsprozess einmal ab.
2. **Zufallsziehungen** – vier ganze Zahlen im Bereich 0–100 werden generiert. Ein Long-Setup erscheint, wenn das erste Paar übereinstimmt, und ein Short-Setup erscheint, wenn das zweite Paar übereinstimmt. Da die Ziehungen unabhängig sind, ist es möglich (wenn auch selten), beide Signale auf derselben Kerze zu generieren.
3. **Ordereignung** – die Strategie platziert nur dann eine neue Order, wenn derzeit keine Position offen ist. Dies hält die Nettoexposition einseitig, anders als das Hedging-Verhalten des MQL-Originals.
4. **Stop/Ziel-Distanzen** – für jede Order werden zwei zusätzliche Zufallszahlen im durch `Min Offset Points` und `Max Offset Points` definierten Bereich erzeugt. Sie bestimmen den Abstand (in Preisschritten) für die Stop-Loss- und Take-Profit-Level rund um den Einstandspreis.
5. **Positionsgrößenbestimmung** – das riskierte Kapital ist durch den `Risk Percent`-Parameter begrenzt. Die Strategie schätzt den Portfolio-Wert (bevorzugt `CurrentValue`, dann `CurrentBalance`, dann `BeginValue`) und teilt das erlaubte Risiko durch den Preisabstand zum Stop. Wenn die Berechnung nicht möglich ist oder null ergeben würde, ist der Fallback das Strategie-`Volume` (Standard: 1 Lot).
6. **Orderausführung** – Marktorders werden über `BuyMarket` / `SellMarket` ausgegeben. Kerzenschlusskurs wird als Proxy für die Einstandskotierung verwendet, da Tick-Level-Bid/Ask-Daten im kerzengetriebenen Workflow nicht verfügbar sind.
7. **Handelsverwaltung** – Stop-Loss- und Take-Profit-Level werden bei jeder fertiggestellten Kerze überprüft. Wenn der Preis ein Level durchdringt, wird die Position durch eine Marktorder geschlossen, was das Verhalten von Schutzorders in der MetaTrader-Version widerspiegelt.

## Parameter
- **Risk Percent** – Prozentsatz des Portfolio-Werts, der verloren gehen kann, wenn der Stop-Loss ausgelöst wird. Werte über null aktivieren risikobasierte Positionsgrößenbestimmung.
- **Min Offset Points / Max Offset Points** – inklusive Grenzen (ausgedrückt in Preisschritten) für die zufällige Auswahl von Stop- und Zieldistanzen. Beide Parameter müssen positiv bleiben; die Implementierung tauscht sie automatisch aus, wenn das Minimum das Maximum übersteigt.
- **Candle Type** – die Datenserie, die den Zufallsmotor antreibt. Jeder `DataType`, der mit `SubscribeCandles` kompatibel ist, kann verwendet werden (standardmäßig Minutenkerzen).

## Unterschiede zur MetaTrader-Version
- **Ereignisquelle** – der MT5-Experte arbeitet bei jedem Tick. Die StockSharp-Strategie wertet die Zufallslotterie bei fertigen Kerzen aus, um dem empfohlenen High-Level-API-Ansatz zu folgen.
- **Hedging** – MetaTrader kann mehrere Positionen auf beiden Seiten ansammeln. Der Port beschränkt sich auf eine einzige Nettoposition (long, short oder flat), da StockSharp-Strategien typischerweise netto abgewickelt werden.
- **Geldverwaltung** – das Original stützte sich auf `CMoneyFixedMargin`. Die C#-Version reproduziert die Idee mit Portfolio-Metriken und prozentualer Risikodimensionierung.
- **Orderplatzierung** – explizite Slippage- und Wiederholungsschleifen sind in StockSharp unnötig und wurden entfernt. Marktorders werden einmal gesendet, nachdem die Umgebung Bereitschaft meldet (`IsFormedAndOnlineAndAllowTrading`).

## Verwendungshinweise
- Sicherstellen, dass das ausgewählte Instrument einen gültigen `PriceStep` enthüllt. Falls keiner verfügbar ist, fällt die Strategie auf einen Schritt von 1 zurück, um die Simulation am Laufen zu halten.
- Da das System intentionell zufällig ist, wird die Performance stark zwischen Backtests variieren. Die Strategie hauptsächlich für Experimente mit Infrastruktur, Risikobehandlung oder Monte-Carlo-artiger Zufälligkeit verwenden.
- Den Kerzen-Zeitrahmen anpassen, um zu steuern, wie häufig Trades erscheinen können. Kürzere Kerzen erhöhen die Anzahl der Lotterien pro Sitzung.
- Die Strategie zeichnet sowohl Kerzen als auch ausgeführte Trades auf einem Chartbereich, wenn Charting verfügbar ist, was hilft zu diagnostizieren, wie oft die Zufallsbedingungen erfüllt werden.

## Konvertierungshinweise
- Originaldatei: `MQL/17744/Pinball machine.mq5`.
- Alle Eingabesteuerungen (Risikoprozent, Stop- und Zielbereiche) in Parameterform gehalten, geeignet für Optimierung innerhalb von StockSharp.
- Zufallssamen verwendet den Plattform-Standard (`Random()`), was dem `MathSrand(GetTickCount())`-Aufruf des MetaTrader-Experten entspricht.
