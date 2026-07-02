# Divergence Trader Basket-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des Expertenberaters „Divergence Trader“ MetaTrader. Es vergleicht zwei einfache gleitende Durchschnitte
berechnet auf konfigurierbaren Preisquellen und misst deren Differenz (Divergenz). Wenn der Abstand zwischen schnell und langsam ist
Wenn die Durchschnittswerte in einen neutralen Korridor fallen, geht der Algorithmus davon aus, dass die Dynamik bald wieder einsetzt, und eröffnet eine Position im
Richtung der vorherrschenden Voreingenommenheit. Die Implementierung verwendet nur abgeschlossene Kerzen aus einem ausgewählten Zeitrahmen und verlässt sich auf die
High-Level-API mit Indikatorbindungen.

## Parameter
- **Lotgröße** – Handelsvolumen, das mit jeder neuen Position übermittelt wird. Der Wert richtet sich nach dem Lautstärkeschritt des Instruments.
- **Schneller SMA-Zeitraum/Preis** – Länge und Preisquelle für den sich schnell bewegenden Durchschnitt.
- **Langsamer SMA Zeitraum / Preis** – Länge und Preisquelle für den langsamen gleitenden Durchschnitt.
- **Kaufschwelle** – minimale positive Divergenz erforderlich, bevor eine Long-Position eröffnet wird.
- **Stay-Out-Schwelle** – maximal zulässige Abweichung für neue Einträge; Werte außerhalb dieses Bereichs deaktivieren den Handel.
- **Take Profit (Pips)** – Gewinnziel ausgedrückt in Pips. Deaktiviert, wenn auf Null gesetzt.
- **Stop Loss (Pips)** – Verlusttoleranz in Pips. Deaktiviert, wenn auf Null gesetzt.
- **Trailing Stop (Pips)** – Trailing-Distanz wird aktiviert, nachdem der Handel profitabel wird. Deaktiviert, wenn Null.
- **Break-Even-Trigger/Puffer (Pips)** – Pip-Gewinn erforderlich, bevor die Position beim Break-Even und optionalem Puffer geschützt wird
Ausgleich des Break-Even-Stops vom Einstiegspreis.
- **Korbgewinn/Korbverlust** – auf Kontoeigenkapital basierende Schwellenwerte, die bei Erreichen alle Positionen abflachen. Verlustkontrolle ist
standardmäßig deaktiviert.
- **Startstunde / Stoppstunde** – Handelsfenster in Ortszeit. Wenn beide Werte gleich sind, funktioniert die Strategie den ganzen Tag.
- **Kerzentyp** – Zeitrahmen, der sowohl für die Signalgenerierung als auch für das Risikomanagement verwendet wird.

## Handelslogik
1. Abonnieren Sie die konfigurierte Kerzenserie und berechnen Sie die schnellen und langsamen einfachen gleitenden Durchschnitte.
2. Arbeiten Sie nur mit fertigen Kerzen, um Intrabar-Rauschen zu vermeiden und nahe am ursprünglichen EA-Verhalten zu bleiben.
3. Verfolgen Sie die Divergenz (schnell minus langsam), die für die zuvor fertige Kerze berechnet wurde:
   - Wenn die Divergenz positiv ist und zwischen der **Kaufschwelle** und der **Stay-Out-Schwelle** bleibt, erteilen Sie eine Marktkauforder.
   - Wenn die Divergenz negativ ist und ihr absoluter Wert innerhalb des Korridors bleibt, erteilen Sie einen Marktverkaufsauftrag.
4. Trades werden außerhalb der erlaubten Zeiten oder wenn die Strategie bereits eine offene Position hat, ignoriert.

## Positionsmanagement
- **Break-even-Kontrolle** – wenn der variable Gewinn den Auslöser erreicht, speichert die Strategie ein Break-even-Stop-Level (optional).
durch den Puffer verschoben). Eine Kerze, die dieses Niveau berührt, schließt die Position.
- **Trailing Stop** – sobald der Gewinn die Trailing-Distanz überschreitet, folgt das Stop-Level immer dem günstigsten Preis
um die konfigurierte Anzahl an Pips dahinter zu bleiben.
- **Take Profit / Stop Loss** – feste Ausstiege, berechnet aus dem Einstiegspreis in Pip-Einheiten.
- **Korbschutz** – Portfolioeigenkapital wird mit den konfigurierten Gewinn- und Verlustgrenzen verglichen. Beide Grenzen erreichen
Schließt die aktuelle Position und storniert aktive Aufträge und emuliert dabei die Routine „CloseEverything“ aus der MQL-Version.

## Nutzungshinweise
- Der Divergenzkorridor ist symmetrisch: Durch die Ausweitung des **Stay-Out-Schwellenwerts** können Geschäfte länger offen bleiben, während er gleichzeitig enger wird
erhöht die Frequenz der Signale.
- Preisquellenoptionen entsprechen StockSharp `CandlePrice`-Werten und ermöglichen die Verwendung von Eröffnung, Schluss, Median oder Typisch
Preise wie in MetaTrader.
- Die Strategie zeichnet Kerzen, sowohl gleitende Durchschnitte als auch ausgeführte Orders, zur Überwachung und Fehlerbehebung in einem Diagrammbereich auf.
- Die Funktionen zur Geldverwaltung hängen von den Portfoliodaten ab. Bei der Ausführung in einer Sandbox ohne Portfoliostatistiken sind Korbkontrollen vorhanden
automatisch ignoriert.
