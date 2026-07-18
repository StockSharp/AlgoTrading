# E Skoch-Strategie für schwebende Orders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **E Skoch-Strategie für schwebende Orders** recreiert den ursprünglichen MetaTrader-Expertenberater, der auf eine neue Bar wartet, die zwei neuesten Hochs und Tiefs sowohl im Trading-Zeitrahmen als auch im Tages-Zeitrahmen analysiert und schwebende Breakout-Orders platziert. Das Ziel ist es, Momentum zu erfassen, wenn der Markt nach einem kurzfristigen Rücksetzer, der durch den Tagestrend bestätigt wird, durch die vorherige Bar bricht.

Die StockSharp-Implementierung behält die ursprünglichen Ideen bei, verwendet jedoch High-Level-API-Funktionen wie Kerzenabonnements, automatische Schutzorders und Strategieparameter. Die C#-Version ist im Ordner `CS/` gespeichert und es wird noch kein Python-Port bereitgestellt.

## Handelslogik

1. Bei jeder abgeschlossenen Kerze ruft die Strategie die Hochs und Tiefs der vorherigen zwei Kerzen im Arbeitszeitrahmen und der vorherigen zwei Tageskerzen ab.
2. Wenn das letzte Tageshoch niedriger ist als das vor zwei Tagen **und** das vorherige Intradayhoch niedriger ist als das davor, platziert die Strategie einen **Buy Stop** über dem letzten Intradayhoch plus einem konfigurierbaren Puffer.
3. Wenn das letzte Tagestief höher ist als das vor zwei Tagen **und** das vorherige Intradaytief höher ist als das davor, platziert die Strategie einen **Sell Stop** unter dem letzten Intradaytief minus einem konfigurierbaren Puffer.
4. Jede schwebende Order setzt individuelle Stop-Loss- und Take-Profit-Level. Wenn ein Einstieg ausgelöst wird, sendet die Strategie sofort Schutz-Stop- und Limit-Orders für die offene Position.
5. Wenn keine Positionen oder Orders aktiv sind, zeichnet die Strategie das aktuelle Eigenkapital als Basislinie auf. Wenn das Kontoeigenkapital um den konfigurierten Prozentsatz relativ zu dieser Basislinie wächst, werden alle Positionen geschlossen und Schutzorders storniert.
6. Optionale Blockierung (`CheckExistingTrade`) verhindert neue Einstiege, während eine Position offen ist, und spiegelt den ursprünglichen Eingabeparameter "CheckTrade" wider.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Primärer Zeitrahmen für Signale. Standard: 1-Stunden-Kerzen. |
| `TakeProfitBuyPips` / `StopLossBuyPips` | Long-seitige Gewinn- und Verlustversätze in Pips. |
| `TakeProfitSellPips` / `StopLossSellPips` | Short-seitige Gewinn- und Verlustversätze in Pips. |
| `IndentHighPips` / `IndentLowPips` | Abstand in Pips vom letzten Hoch oder Tief zur Platzierung von Stop-Orders. |
| `CheckExistingTrade` | Wenn true, werden neue Orders übersprungen, während eine Position offen ist. |
| `PercentEquity` | Prozentualer Eigenkapitalgewinn, der für den Ausstieg aus allen Positionen erforderlich ist. |
| `Volume` | Ordergröße (Standard 0,01 Lot, um dem ursprünglichen Expertenberater zu entsprechen). |

## Risikomanagement

- Buy-Stop-Orders platzieren einen Stop-Loss unter dem Einstiegspreis und einen Take-Profit darüber.
- Sell-Stop-Orders platzieren einen Stop-Loss über dem Einstiegspreis und einen Take-Profit darunter.
- Schutzorders werden automatisch storniert, wenn die Position schließt oder wenn ein neuer Schutz-Set erstellt wird.
- Die Eigenkapital-Wachstumsprüfung fungiert als globaler "Sicherungsautomat", um Gewinne zu sichern, bevor der Handel fortgesetzt wird.

## Hinweise

- Die Strategie erfordert sowohl den Handelszeitrahmen als auch Tageskerzen, stellen Sie also sicher, dass Daten für beide Abonnements in Designer oder während Backtests verfügbar sind.
- Die Pip-Konvertierung passt sich automatisch an Symbole an, die Bruch-Pip-Preise verwenden (3 oder 5 Dezimalstellen), indem der Preisschritt mit 10 multipliziert wird.
- Die Logik geht von einer einzigen aggregierten Position aus; simultane Long- und Short-Exposition wird absichtlich vermieden, wenn `CheckExistingTrade` aktiviert ist.
