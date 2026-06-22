# Elli Ichimoku ADX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Strategie ist ein C#-Port des MetaTrader-5-Experten "Elli" (barabashkakvn's edition). Sie kombiniert die Ichimoku-Kinko-Hyo-Struktur mit einem Average-Directional-Index-(+DI)-Ausbruchsfilter. Trades werden nur geöffnet, wenn ein starker direktionaler Impuls gleichzeitig durch die Ichimoku-Linienausrichtung und einen plötzlichen Anstieg des positiven Richtungsindex bestätigt wird.

Die StockSharp-Implementierung behält das ursprüngliche Verhalten der Arbeit mit zwei Kerzenströmen bei: Die Ichimoku-Analyse wird auf einem höheren Zeitrahmen (Standard 1 Stunde) durchgeführt, während ADX auf einer schnelleren Serie (Standard 1 Minute) ausgewertet wird. Orders werden mit einem festen Schutz-Stop und Ziel in Preisschritten eingegeben, identisch mit dem ursprünglichen Expertenberater.

## Indikatoren und Daten
- **Ichimoku** (Tenkan 19, Kijun 60, Senkou Span B 120 standardmäßig).
- **Average Directional Index (ADX)**, nur die +DI-Linie wird wie im Quellcode verwendet.
- Optionale Chartbereiche zeigen die Kerzenserie, die Ichimoku-Wolke und die ADX-Linie.

Zwei unabhängige Kerzenabonnements werden erstellt:
1. `IchimokuCandleType` (Standard 1 Stunde) – treibt Ichimoku-Berechnungen und generiert Handelsentscheidungen.
2. `AdxCandleType` (Standard 1 Minute) – speist den ADX-Indikator und liefert aktuelle/vorherige +DI-Werte.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `TakeProfitPoints` | 60 | Take-Profit-Distanz in Preisschritten. Auf 0 setzen zum Deaktivieren. |
| `StopLossPoints` | 30 | Stop-Loss-Distanz in Preisschritten. Auf 0 setzen zum Deaktivieren. |
| `TenkanPeriod` | 19 | Länge der Ichimoku-Tenkan-sen (Konversionslinie). |
| `KijunPeriod` | 60 | Länge der Ichimoku-Kijun-sen (Basislinie). |
| `SenkouSpanBPeriod` | 120 | Länge der Ichimoku-Senkou-Span-B-Linie. |
| `AdxPeriod` | 10 | Periode für den ADX-Indikator. |
| `PlusDiHighThreshold` | 13 | Schwellenwert, den der aktuelle +DI-Wert übersteigen muss. |
| `PlusDiLowThreshold` | 6 | Schwellenwert, unter dem der vorherige +DI-Wert bleiben muss. |
| `BaselineDistanceThreshold` | 20 | Minimaler Tenkan/Kijun-Spread (in Preisschritten) zur Bestätigung des Momentums. |
| `IchimokuCandleType` | 1-Stunden-Kerzen | Kerzenserie für die Ichimoku-Auswertung. |
| `AdxCandleType` | 1-Minuten-Kerzen | Kerzenserie für die ADX-Berechnung. |

## Handelslogik
1. Eine fertige Ichimoku-Kerze abwarten.
2. Sicherstellen, dass ADX mindestens zwei fertige Werte hat und die letzte Lesung einen +DI-Ausbruch erzeugte (`vorheriges +DI < PlusDiLowThreshold` und `aktuelles +DI > PlusDiHighThreshold`).
3. Den Tenkan/Kijun-Spread in Preisschritte umrechnen und überprüfen, ob er `BaselineDistanceThreshold` übersteigt.
4. Alle Orders werden blockiert, wenn bereits eine offene Position vorhanden ist.
5. **Kaufen** wenn:
   - Tenkan > Kijun.
   - Kijun > Senkou Span A.
   - Senkou Span A > Senkou Span B (bullische Wolke).
   - Schlusskurs > Kijun.
6. **Verkaufen** wenn die umgekehrte Ausrichtung beobachtet wird (Tenkan < Kijun < Senkou Span A < Senkou Span B und der Schluss unter Kijun liegt).
7. Positionsausstiege verlassen sich auf den konfigurierten Schutz-Stop und das Ziel über `StartProtection`. Kein diskretionärer Ausstieg wird ausgelöst; dies spiegelt den ursprünglichen EA wider, der auf Stops/Ziele oder manuelle Eingriffe wartete.

## Risikomanagement
`StartProtection` wird einmal beim Start aufgerufen. Wenn entweder Stop oder Ziel null ist, wird der entsprechende Schutz weggelassen. Orders werden mit Marktausführung gesendet (`BuyMarket`/`SellMarket`), was der MQL-Implementierung entspricht, die Marktorders mit angehängtem SL/TP verwendete.

## Implementierungshinweise
- Nur der positive Richtungsindex wird für Long- und Short-Signale verwendet, was die Logik des MQL5-Codes repliziert (der ursprüngliche Autor hat den -DI-Zweig auskommentiert).
- Die Strategie verfolgt die Chikou-Spanne nicht explizit; stattdessen wird die Wolkenausrichtung durch Vergleich von Senkou Span A und B validiert.
- Interne Felder speichern die letzten zwei +DI-Werte ohne `GetValue` aufzurufen, gemäß den High-Level-API-Richtlinien.
- Wenn beide Kerzenparameter identisch sind, wird ein einzelnes Abonnement für Ichimoku und ADX wiederverwendet, um den Overhead zu reduzieren.

## Verwendungstipps
- `AdxCandleType` schneller als `IchimokuCandleType` halten, um die MT5-Version zu emulieren (z.B. M1 ADX vs. H1 Ichimoku).
- `BaselineDistanceThreshold` bei hochvolatilen Instrumenten erhöhen, um eine größere Tenkan/Kijun-Trennung zu verlangen.
- Da der Experte jeweils nur eine Position öffnet, die Strategie mit Risikokontrollen auf Portfolio-Ebene kombinieren, wenn mehrere Symbole gehandelt werden.
