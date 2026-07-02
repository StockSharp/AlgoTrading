# Teststrategie für Kerzenmuster
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Candle Patterns Test Strategy** ist eine StockSharp High-Level-Konvertierung des ursprünglichen MetaTrader 5 Expertenberaters *CandlePatternsTest EA*. Die Strategie durchsucht abgeschlossene Kerzen nach einer kuratierten Liste klassischer japanischer Kerzenformationen und reagiert, indem sie Long- oder Short-Positionen eingeht, wenn bullische oder bärische Strukturen auftreten. Die Konvertierung konzentriert sich auf die diskretionäre Musterlogik des Quellroboters und nutzt gleichzeitig StockSharp Risikokontrollen und Datenabonnements API.

## Handelslogik

1. **Kerzenabonnement** – die Strategie abonniert den konfigurierten Kerzentyp und wartet auf fertige Balken, bevor sie die Mustererkennung durchführt.
2. **Durchschnittskörperfilter** – ein einfacher gleitender Durchschnitt von Kerzenkörpern dient als dynamische Normalisierung. Nur Muster, deren konstituierende Kerzen diesen Durchschnitt überschreiten, gelten als gültig und spiegeln die Funktion `AvgBody` der MQL-Implementierung wider.
3. **Mustererkennung** – der Detektor prüft auf:
   - Drei weiße Soldaten / drei schwarze Krähen
   - Durchdringende Linie / dunkle Wolkendecke
   - Morgen-Doji-Stern / Abend-Doji-Stern
   - Bullisches und bärisches Engulfing
   - Bullisches und bärisches Harami
   - Besprechungslinien
4. **Eintrittsmanagement** – sobald ein bullisches Muster bestätigt ist, eröffnet die Strategie eine Marktkauforder; Abwärtstrends lösen einen Marktverkaufsauftrag aus. Gegenüberliegende Signale kehren automatisch die aktuelle Position um.
5. **Exit-Management** – schützende Stop-Loss- und Take-Profit-Werte werden vom durchschnittlichen Kerzenkörper abgeleitet und für jede fertige Kerze verfolgt. Wenn der Preis einen dieser Schwellenwerte erreicht, wird die Position geschlossen.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Datentyp der zu abonnierenden Kerzen (Standard: 1-Stunden-Zeitrahmen). |
| `AverageBodyPeriod` | Anzahl der verwendeten Kerzen für die durchschnittliche Körperlänge. Steuert die Musternormalisierung. |
| `EnableBullishPatterns` | Aktiviert oder deaktiviert lange Einträge. |
| `EnableBearishPatterns` | Aktiviert oder deaktiviert kurze Einträge. |
| `StopLossFactor` | Auf den durchschnittlichen Körper angewendeter Multiplikator für die Stop-Loss-Distanz. |
| `TakeProfitFactor` | Auf den durchschnittlichen Körper angewendeter Multiplikator für die Take-Profit-Distanz. |

Alle Parameter werden über `StrategyParam<T>` verfügbar gemacht, um die GUI-Konfiguration und Optimierungsausführungen zu unterstützen.

## Diagramme

Wenn ein Diagrammbereich verfügbar ist, wird die Strategie wie folgt dargestellt:

- Die abonnierten Kerzen
- Der gleitende Schlusskursdurchschnitt, der für den Trendkontext verwendet wird
- Ausgeführte Trades zur visuellen Überprüfung

## Unterschiede zum Original EA

- Nachrichtenfilter, Zeitfenster, Absicherungsschalter und Trailing-Grid-Management, die in der ursprünglichen MQ5-Datei vorhanden sind, wurden absichtlich weggelassen, um sich auf den Kern des Candlestick-Musters zu konzentrieren.
- Das Risikomanagement wird auf ein symmetrisches Stop/Ziel-Modell vereinfacht, das aus der Kerzenvolatilität abgeleitet wird.
- Die StockSharp-Version nutzt die Positionsverwaltung des Frameworks und `BuyMarket`/`SellMarket`-Helfer anstelle manueller Bestelltickets.

## Nutzungshinweise

- Legen Sie den Parameter `CandleType` so fest, dass er mit der Marktsitzung übereinstimmt, die Sie analysieren möchten. Höhere Zeitrahmen erzeugen weniger, aber stärkere Signale.
- Passen Sie `AverageBodyPeriod` so an, dass der durchschnittliche Körper in etwa der jüngsten Volatilität entspricht. Ein kleinerer Wert reagiert schneller, kann aber das Rauschen verstärken.
- `StopLossFactor` und `TakeProfitFactor` können optimiert werden, um dem Risikoprofil des Instruments zu entsprechen.

## Anforderungen

- StockSharp-Umgebung mit Marktdaten-Feed, der den konfigurierten Kerzentyp generieren kann.
- Die Strategie erwartet sequentielle, nicht überlappende Kerzenserien. Stellen Sie sicher, dass das ausgewählte Board regelmäßige Balkenaktualisierungen unterstützt.
