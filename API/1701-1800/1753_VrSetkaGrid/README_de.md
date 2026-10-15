# VR Setka Gitter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Implementierung des MetaTrader-Gittersystems "VR---SETKAa3hM". Sie eröffnet eine Abfolge von Kauf- oder Verkaufsorders basierend auf der prozentualen Abweichung vom Tagesbereich und erhöht optional das Volumen mithilfe eines Martingale-Multiplikators. Der durchschnittliche Einstiegspreis aller offenen Orders wird verfolgt, um ein einheitliches Take-Profit-Ziel zu setzen.

## Parameter
- `Distance`: Preisabstand in Punkten zwischen den Gitterniveaus.
- `TakeProfit`: Gewinnziel in Punkten für die Erstorder.
- `Correction`: Zusätzlicher Gewinn in Punkten, der zum Durchschnittspreis addiert wird, wenn mehr als eine Order offen ist.
- `SignalPercent`: Prozentschwelle zur Erkennung der Abweichung vom Tagesbereich.
- `UseMartingale`: Volumen mit der Anzahl offener Orders multiplizieren.
- `CandleType`: Kerzen-Zeitrahmen für Signalberechnungen.

## Logik
1. Wenn eine abgeschlossene Kerze erscheint, wird der aktuelle Schlusskurs in Relation zum Tageshoch und -tief berechnet.
2. Wenn die vorherige Kerze bullisch war und der Schlusskurs ausreichend unter dem Tageshoch liegt, wird ein Kaufgitter gestartet oder fortgesetzt.
3. Wenn die vorherige Kerze bärisch war und der Schlusskurs ausreichend über dem Tagestief liegt, wird ein Verkaufsgitter gestartet oder fortgesetzt.
4. Zusätzliche Orders werden platziert, wenn der Preis um `Distance` Punkte gegen die Position läuft.
5. Sobald der Preis zum durchschnittlichen Einstiegspreis plus `Correction` für Käufe oder minus `Correction` für Verkäufe zurückkehrt, werden alle Positionen mit einer Market-Order geschlossen.
