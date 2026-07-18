# Triple-Top-Triple-Bottom-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Triple-Top-Triple-Bottom-Strategie** ist ein Port des gleichnamigen MetaTrader-Expert-Advisors. Das ursprüngliche System kombiniert mehrere Bestätigungsebenen (Trendrichtung, Momentum-Stärke und einen MACD-Filter), bevor es in den Markt eintritt. Diese StockSharp-Implementierung behält dieselben Kernideen bei und stellt die wichtigen Schwellen als Strategieparameter bereit.

## Kernlogik

1. **Trendfilter:** Zwei linear gewichtete gleitende Durchschnitte (LWMA), berechnet auf dem typischen Preis (H+L+C)/3, definieren die Handelsrichtung. Die schnelle LWMA muss über der langsamen liegen, um Long-Trades zu erlauben, und darunter, um Short-Trades zu erlauben.
2. **Momentum-Bestätigung:** Der eingebaute Momentum-Indikator mit konfigurierbarer Rückblicklänge muss innerhalb der letzten drei abgeschlossenen Kerzen mindestens um die benutzerdefinierte Schwelle vom neutralen Niveau 100 abweichen. Der EA verlangte dasselbe Verhalten durch Analyse vorheriger Momentum-Werte, und diese Validierung wird gespiegelt, um Einstiege in flachen Märkten zu vermeiden.
3. **MACD-Filter:** Ein klassischer MACD-Signallinienfilter 12/26/9 verhindert Trades gegen einen starken Trend. Die Strategie kauft nur, wenn die MACD-Linie über der Signallinie liegt, und verkauft, wenn sie darunter liegt.
4. **Risikomanagement:** Marktorders werden mit Stop-Loss- und Take-Profit-Zielen geschützt, die in absoluten Preiseinheiten gemessen werden. Die Parameter sind optional; null deaktiviert die jeweilige Order. Der Code schließt die Position außerdem, wenn der entgegengesetzte Risikoschwellenwert während der Kerzenverarbeitung erreicht wird.

## Parameter

- **Entry Candle:** `DataType`, der den Zeitrahmen der Arbeitskerzen definiert.
- **Fast LWMA / Slow LWMA:** Längen der schnellen und langsamen Trendfilter.
- **Momentum Period / Momentum Threshold:** Rückblick für den Momentum-Indikator und minimale Abweichung von 100, die eine Trade-Idee bestätigt.
- **Stop Loss / Take Profit:** Schutzdistanzen in absoluten Preiseinheiten; sie werden außerdem über `SetStopLoss` und `SetTakeProfit` als native Schutzorders gesendet, sodass die Risikokontrolle auch greift, wenn die Strategiesitzung stoppt.

## Unterschiede zur MQL-Version

- Alle zusätzlichen Geldmanagement-Funktionen (Lot-Multiplikatoren, Equity-Schutz, Kerzen-Trailing, Break-even und manuelle Trendlinienprüfungen) wurden ausgelassen, weil die StockSharp-High-Level-API bereits Werkzeuge zur Positionsgrößenbestimmung bietet und die im ursprünglichen EA verwendeten grafischen Objekte MetaTrader-spezifisch sind.
- Risikoschwellen werden in absoluten Preiseinheiten statt in Pips ausgedrückt. Dies hält die Implementierung brokerneutral; Benutzer können ihre gewünschte Pip-Distanz leicht umrechnen, indem sie die Pip-Größe des Brokers mit der gewünschten Pip-Anzahl multiplizieren.
- Die Chartausgabe verwendet StockSharp-Bereiche für Preis-Kerzen, gleitende Durchschnitte, Momentum und MACD-Indikatoren.

## Nutzungshinweise

1. Binden Sie die Strategie an ein Instrument und konfigurieren Sie vor dem Start den gewünschten Kerzentyp.
2. Passen Sie die Momentum-Schwelle und Stop-Distanzen an die Volatilität des Instruments an.
3. Die Strategie handelt eine einzelne Nettoposition. Wenn ein Gegensignal erscheint, wird die aktuelle Exposure zuerst geschlossen, wodurch überlappende Trades verhindert werden.

Der Code ist vollständig auf Englisch kommentiert und folgt den im Repository bereitgestellten StockSharp-High-Level-API-Richtlinien.
