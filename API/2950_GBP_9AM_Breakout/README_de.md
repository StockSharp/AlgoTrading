# GBP 9-Uhr-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **GBP 9-Uhr-Ausbruch-Strategie** repliziert den klassischen MetaTrader-Expertenberater "GBP9AM" in StockSharp. Das System bereitet einen Straddle rund um die Londoner Eröffnung (9:00 Ortszeit) vor, indem es Buy-Stop- und Sell-Stop-Orders in konfigurierbaren Abständen vom aktuellen Preis platziert. Ziel ist es, die Impulsbewegung nach der Eröffnung zu erfassen und dabei ein diszipliniertes Risikomanagement durch Stop-Loss- und Take-Profit-Niveaus in Pips einzuhalten.

## Handelslogik

1. Die Strategie überwacht abgeschlossene Kerzen eines konfigurierbaren Zeitrahmens (standardmäßig 1 Minute), um mit Börsenzeitstempeln zu arbeiten.
2. Jeder neue Handelstag setzt den Einrichtungszustand zurück, sodass pro Sitzung nur ein Straddle vorbereitet wird.
3. Sobald die Kerzenzeit die konfigurierten Werte "Look Hour" und "Look Minute" erreicht, führt die Strategie folgendes durch:
   - Alle noch aktiven Orders werden storniert und offene Positionen geschlossen, um Konflikte zu vermeiden.
   - Pip-angepasste Einstiegs-, Stop-Loss- und Take-Profit-Preise werden anhand des Preisschritts des Instruments berechnet.
   - Sowohl eine Buy-Stop- als auch eine Sell-Stop-Order wird in den angegebenen Pip-Abständen vom letzten Schlusskurs platziert.
4. Wenn eine Seite ausgeführt wird, wird die entgegengesetzte ausstehende Order sofort storniert. Die Strategie verfolgt dann die Kursentwicklung, um die Position zu schließen, sobald der Stop-Loss- oder Take-Profit-Level innerhalb des Tages erreicht wird.
5. Eine optionale tägliche "Close Hour" zwingt die Strategie, Positionen zu glätten und ausstehende Orders am Ende der Londoner Sitzung zu entfernen.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `Volume` | Ordergröße für beide Seiten des Straddles.
| `LookHour` | Börsenstunde (0-23), die 9 Uhr London-Zeit in Ihrem Datenfeed entspricht.
| `LookMinute` | Minuten-Offset innerhalb der Look Hour, wann Orders vorbereitet werden sollen.
| `CloseHour` | Stunde, zu der alle Positionen und Orders zwangsweise geschlossen werden.
| `UseCloseHour` | Aktiviert oder deaktiviert das automatische Schließverhalten zur festgelegten Stunde.
| `TakeProfitPips` | Abstand in Pips vom Einstiegspreis zum Gewinnziel für beide Richtungen.
| `BuyDistancePips` | Pip-Abstand über dem aktuellen Preis für die Buy-Stop-Order.
| `SellDistancePips` | Pip-Abstand unter dem aktuellen Preis für die Sell-Stop-Order.
| `BuyStopLossPips` | Stop-Loss-Abstand in Pips für Long-Positionen.
| `SellStopLossPips` | Stop-Loss-Abstand in Pips für Short-Positionen.
| `CandleType` | Kerzen-Abonnement für Timing und Exit-Management (Standard: 1-Minuten-Zeitrahmen).

Alle Pip-Abstände passen sich automatisch an 3- oder 5-stellige FX-Kurse an, indem der Börsen-Preisschritt bei Bedarf mit zehn multipliziert wird, was den ursprünglichen Expertenberater widerspiegelt.

## Risikomanagement

- Die Strategie gibt stets symmetrische Stop-Loss- und Take-Profit-Ziele um den Auslösepreis aus, um ein ausgewogenes Risikoprofil zu gewährleisten.
- Die Tagesend-Liquidation stellt sicher, dass das Konto keine Overnight-Exposition trägt, es sei denn, der Parameter `UseCloseHour` ist deaktiviert.
- Da Orders nur einmal täglich neu gesetzt werden, vermeidet die Strategie Übertrading während Seitwärtsbewegungen.

## Verwendungshinweise

1. Stellen Sie `LookHour` so ein, dass 9 Uhr London-Zeit in der Zeitzone Ihres Brokers passt. Wenn der Feed beispielsweise UTC+1 ist, verwenden Sie `LookHour = 10`.
2. Kalibrieren Sie die Pip-Abstände entsprechend der aktuellen Volatilität von GBP/USD oder Ihrem bevorzugten GBP-Paar.
3. Setzen Sie die Strategie auf FX-Symbolen ein, die zuverlässige Bid/Ask- und Preisschritt-Metadaten bereitstellen, damit die Pip-Berechnungen genau bleiben.
4. Achten Sie auf die Broker-Margen: Größere `Volume`-Werte erfordern möglicherweise Anpassungen des Konto-Hebels, genau wie bei der ursprünglichen MQL-Version.

## Dateien

- `CS/Gbp9AmBreakoutStrategy.cs` – C#-Implementierung mit der High-Level-API von StockSharp.
- `README.md` – Englische Dokumentation (diese Datei).
- `README_ru.md` – Russische Dokumentation.
- `README_zh.md` – Chinesische Dokumentation.

Eine Python-Implementierung wird gemäß den Projektanforderungen absichtlich nicht bereitgestellt.
