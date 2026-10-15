# Volatilitäts-Pivot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Volatilitäts-Pivot-Strategie ist ein StockSharp-Port des ursprünglichen Expert Advisors **Exp_VolatilityPivot.mq5**. Sie recreiert den benutzerdefinierten Volatility Pivot-Indikator, indem zwei adaptive Stop-Linien projiziert werden, die dem Preis mithilfe von Average True Range (ATR)-Volatilität oder einer festen Preisabweichung folgen. Wenn sich die Tendenz dreht, gibt der Indikator einmalige Ausbruchs-Pfeile aus, die Positionsumkehrungen auslösen. Die Strategie kann diesen Signalen folgen (`WithTrend`) oder gegen sie handeln (`CounterTrend`), was Flexibilität für Ausbruchs- oder Mean-Reversion-Stile bietet.

Im Gegensatz zur MQL-Implementierung stützt sich diese Version ausschließlich auf abgeschlossene Kerzen, die von `CandleType` geliefert werden. Der ATR-Modus multipliziert einen geglätteten ATR (EMA des ATR) mit `AtrMultiplier`, während der Preismodus den rohen `DeltaPrice`-Versatz verwendet. Die resultierenden Pivot-Linien definieren bullishe und bearishe Trailing-Levels, die Einstiege und Ausstiege steuern.

## Marktdaten und Indikatoren
- **Primäre Kerzen (`CandleType`)** – alle Berechnungen werden in diesem Zeitrahmen durchgeführt. Der Standard ist ein 4-Stunden-Balken, um dem Quell-Expert Advisor zu entsprechen.
- **ATR + EMA-Glättung** – im `Atr`-Modus verarbeitet die Strategie einen `AverageTrueRange` mit Länge `AtrPeriod` und glättet ihn dann durch einen `ExponentialMovingAverage` der Länge `SmoothingPeriod`.
- **Preisabweichungsmodus** – im `PriceDeviation`-Modus ist der Trailing-Versatz der feste `DeltaPrice`-Betrag, der deterministische Stop-Abstände ermöglicht, wenn keine Volatilitätsglättung gewünscht wird.
- **Pivot-Zustandsverfolgung** – die Strategie hält die neuesten bullishen/bearishen Trail-Werte und löst "Signale" nur in der Kerze aus, in der der Trail von einer Seite des Preises auf die andere wechselt, was die Indikatorbuffer der MQL-Version widerspiegelt.

## Handelslogik
1. **Pivot-Berechnung** – für jede abgeschlossene Kerze aktualisiert die Strategie den Trailing-Stop-Preis gemäß den Volatility Pivot-Regeln. Ein bullisher Trail ist aktiv, wenn der Preis über dem berechneten Stop schließt; ein bearisher Trail ist aktiv, wenn er darunter schließt.
2. **Signalerkennung** – ein neues bullishes (bearishes) Signal wird ausgelöst, wenn der bullishe (bearishe) Trail nach inaktivem Zustand in der vorherigen Kerze aktiv wird. Der `SignalBar`-Parameter verzögert die Ausführung um die angeforderte Anzahl abgeschlossener Balken und repliziert die `SignalBar`-Eingabe des MQL-Skripts.
3. **Richtungsfilter (`TradeDirection`)** – bei Einstellung auf `WithTrend` kauft die Strategie bei bullishen Signalen und verkauft bei bearishen Signalen. Bei Einstellung auf `CounterTrend` wird die Interpretation invertiert: Bullishe Pfeile schließen Shorts und eröffnen neue Shorts, und umgekehrt.
4. **Einstiegsberechtigungen** – `EnableBuyEntries` und `EnableSellEntries` steuern, ob neue Long- oder Short-Positionen eröffnet werden dürfen.
5. **Ausstiegsberechtigungen** – `AllowLongExits` und `AllowShortExits` steuern, ob bestehende Positionen durch direkte Signale oder durch den entgegengesetzten Trail geschlossen werden dürfen.
6. **Positionsanpassung** – die Strategie zielt auf eine Nettoposition von `+Volume` für Longs, `-Volume` für Shorts und `0` beim Flachlegen. Orders werden automatisch dimensioniert, um jede entgegengesetzte Exposure zu schließen, bevor die neue Richtung eingegangen wird.
7. **Schutzstops** – optionale `StopLoss`- und `TakeProfit`-Abstände (in absoluten Preiseinheiten ausgedrückt) überwachen jede abgeschlossene Kerze. Wenn das Hoch/Tief der Kerze diese Levels verletzt, beendet die Strategie sofort die Position.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `CandleType` | Kerzenserie für die Indikatorverarbeitung und Ausführung. | 4-Stunden-Kerzen |
| `AtrPeriod` | Länge der ATR-Komponente. | 100 |
| `SmoothingPeriod` | EMA-Glättungslänge, angewendet auf ATR-Werte. | 10 |
| `AtrMultiplier` | Multiplikator, angewendet auf den geglätteten ATR. | 3.0 |
| `DeltaPrice` | Fester Preisversatz bei `PivotMode = PriceDeviation`. | 0.002 |
| `PivotMode` | Wählt zwischen ATR-basierten oder festen Abweichungs-Pivots. | `Atr` |
| `TradeDirection` | Folgt (`WithTrend`) oder verblasst (`CounterTrend`) Pivot-Ausbrüchen. | `WithTrend` |
| `SignalBar` | Anzahl abgeschlossener Balken zum Warten vor dem Agieren auf ein Signal. | 1 |
| `EnableBuyEntries` | Neue Long-Positionen erlauben. | `true` |
| `EnableSellEntries` | Neue Short-Positionen erlauben. | `true` |
| `AllowLongExits` | Bestehende Long-Positionen bei bearishen Bedingungen schließen erlauben. | `true` |
| `AllowShortExits` | Bestehende Short-Positionen bei bullishen Bedingungen schließen erlauben. | `true` |
| `StopLoss` | Optionaler Stop-Loss-Abstand (absolute Preiseinheiten). Auf `0` setzen zum Deaktivieren. | 0 |
| `TakeProfit` | Optionaler Take-Profit-Abstand (absolute Preiseinheiten). Auf `0` setzen zum Deaktivieren. | 0 |

> **Hinweis:** Die StockSharp-Eigenschaft `Strategy.Volume` definiert die Positionsgröße. Vor dem Starten der Strategie konfigurieren, um der Kontraktgröße oder Aktiengröße des Instruments zu entsprechen.

## Verwendungsrichtlinien
1. Die Strategie an das gewünschte `Security` und `Portfolio` anhängen und `Volume` auf die beabsichtigte Losgröße setzen.
2. Sicherstellen, dass die Datenquelle den ausgewählten `CandleType` liefern kann. Ohne kontinuierlichen Feed abgeschlossener Kerzen können sich ATR-Glättung und Signalverzögerungslogik nicht bilden.
3. `PivotMode` je nach Marktverhalten wählen: ATR-Modus passt sich der Volatilität an, Preisabweichungsmodus hält den Trail fest.
4. `SignalBar` anpassen, um das genaue Timing des ursprünglichen Expert Advisors zu reproduzieren (standardmäßig 1 Balken Verzögerung). Auf `0` setzen, um auf der neuesten abgeschlossenen Kerze auszuführen.
5. Bei Verwendung von `StopLoss`/`TakeProfit` die Abstände an die Instrumentenvolatilität kalibrieren (sie sind absolute Preise, keine Punkte oder Prozentzahlen).
6. Logs auf informative Meldungen über Einstiege, Ausstiege und durch Pivot-Änderungen ausgelöste Schutzstops überwachen.

## Unterschiede zum Original Expert Advisor
- Geldverwaltungsoptionen basierend auf Kontostand/freier Marge wurden entfernt. Die Positionsgröße wird ausschließlich über `Strategy.Volume` gesteuert.
- Preis-"Abweichung" und manuelle Zeitsynchronisation aus der MQL-Hilfsbibliothek sind unnötig, da StockSharp Market-Orders auf abgeschlossenen Kerzen verwendet.
- Benachrichtigungsfunktionen, globale Variablen und manuelle Historienladung aus dem MQL-Skript werden weggelassen.
- Schutzstop- und Take-Profit-Handhabung ist auf kerzenbasierte Prüfungen vereinfacht; es gibt keine Intra-Bar-Orderplatzierung.

## Empfohlene Erweiterungen
- Tagesession-Filter oder Volatilitätsfilter hinzufügen, um den Handel während liquiditätsschwacher Stunden zu pausieren.
- Die Strategie mit Trailing-Stop-Management erweitern, das die Pivot-Linien widerspiegelt, oder die berechneten Linien zur Visualisierung in ein Diagramm exportieren.
- Portfolio-weite Risikokontrollen einbeziehen, wenn mehrere Instrumente dieselbe Strategieinstanz verwenden.
