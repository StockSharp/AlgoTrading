# Scalper EMA Einfache Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Scalper EMA Simple Strategy** ist eine Umsetzung des MetaTrader Expert Advisors `ScalperEMAEASimple`. Es verwendet eine Kombination aus schnellen/langsamen exponentiellen gleitenden Durchschnitten, einen stochastischen Oszillator und einen Average Directional Index (ADX)-Filter, um kurzlebige Pullback-Einträge innerhalb eines bestehenden Trends zu identifizieren. Die Strategie ist für das Intraday-Scalping bei liquiden FX-Paaren konzipiert, kann aber auf jedes Instrument angewendet werden, bei dem ein Pip-basiertes Risikomanagement sinnvoll ist.

Die Implementierung folgt dem StockSharp High-Level API und wertet nur fertige Kerzen aus. Alle Berechnungen werden inkrementell durchgeführt, ohne dass historische Daten erneut verarbeitet werden, wodurch die Logik für den Live-Handel geeignet ist.

## Indikatorstapel

- **Schneller EMA (`FastEmaPeriod`)** – erkennt kurzfristige Dynamik.
- **Langsamer EMA (`SlowEmaPeriod`)** – definiert die vorherrschende Trendrichtung.
- **Stochastic Oszillator (`StochasticLength`, `StochasticKPeriod`, `StochasticDPeriod`)** – verfolgt Momentumumkehrungen in der Nähe von überverkauften/überkauften Grenzen.
- **Durchschnittlicher Richtungsindex** – lehnt Trades ab, wenn der Trend übermäßig stark wird (ADX über `AdxThreshold`).

Der stochastische Oszillator löst immer dann ein Bestätigungssignal aus, wenn die %K-Linie wieder über das überverkaufte Niveau (Long-Setups) oder unter das überkaufte Niveau (Short-Setups) kreuzt. Das EMA-Paar stellt den Richtungsfilter bereit, und die ADX-Komponente stellt sicher, dass Einträge auf ruhige Retracements und nicht auf außer Kontrolle geratene Trends beschränkt sind.

## Eingabelogik

1. Die Kerze muss auf der Trendseite des langsamen EMA schließen und der schnelle EMA muss mit dieser Richtung übereinstimmen (`fast > slow` für Long-Positionen, `fast < slow` für Short-Positionen).
2. Der Abstand zwischen der Kerze und dem langsamen EMA muss kleiner als der Kerzenbereich und enger als die drei vorherigen Abstände sein. Durch dieses Verhalten wird die Pullback-Erkennungsschleife aus dem ursprünglichen MQL-Code neu erstellt.
3. Entweder kreuzt der Kerzenkörper den schnellen EMA oder der schnelle EMA selbst kreuzt den langsamen EMA. Diese Bedingung fungiert als Ausbruchsauslöser.
4. Der stochastische Oszillator muss die Dynamik bestätigen, indem er innerhalb der letzten `ConditionWindowBars` Kerzen aus der Extremzone zurückkehrt.
5. ADX muss unter `AdxThreshold` bleiben, um Trades zu verhindern, wenn die Volatilität stark zunimmt.
6. Zwischen zwei aufeinanderfolgenden Signalen derselben Richtung müssen mindestens `SignalCooldownBars` Kerzen passieren.

Wenn alle Prüfungen erfolgreich sind, schließt die Strategie alle gegenläufigen Positionen und eröffnet eine neue Marktorder in der erkannten Richtung.

## Exit-Logik und Risikokontrollen

- Ein anfänglicher Stop-Loss wird bei `StopLossPips` (umgerechnet in einen Preis unter Verwendung der Pip-Größe des Instruments) vom Einstiegspreis platziert.
- Ein Trailing Stop hält automatisch einen Abstand von `TrailingDistancePips` ein, sobald der nicht realisierte Gewinn `TrailingActivationPips` erreicht.
- Gegensätzliche Signale erzwingen eine flache Position, bevor ein neuer Handel etabliert wird.

Alle Schutzanordnungen werden über den `SetStopLoss`-Helper von StockSharp verwaltet, um die Risikokontrollen mit dem aktuellen Positionsvolumen synchron zu halten.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `Volume` | Basishandelsvolumen für jedes Signal. Die Strategie fügt automatisch das vorhandene Risiko hinzu, um bei einem Richtungswechsel eine vollständige Umkehr sicherzustellen. |
| `FastEmaPeriod` / `SlowEmaPeriod` | Periodenlängen für die exponentiellen gleitenden Durchschnitte. |
| `StochasticLength`, `StochasticKPeriod`, `StochasticDPeriod` | Stochastic-Oszillatorkonfiguration, die die ursprünglichen EA-Standardeinstellungen widerspiegelt. |
| `StochasticOversold` / `StochasticOverbought` | Extreme Niveaus, die die Retracement-Zonen definieren. |
| `AdxThreshold` | Maximal zulässiger ADX-Wert, bevor Trades abgelehnt werden. |
| `SignalCooldownBars` | Mindestbalken zwischen aufeinanderfolgenden Signalen in derselben Richtung. |
| `ConditionWindowBars` | Anzahl der Balken, während derer Retracement, EMA-Ausbruch und stochastische Bestätigung übereinstimmen müssen. |
| `StopLossPips` | Anfängliche Stop-Loss-Distanz, ausgedrückt in Pips. |
| `TrailingDistancePips` | Abstand, der durch den Trailing Stop eingehalten wird, sobald er aktiviert ist. |
| `TrailingActivationPips` | Gewinnschwelle, die den Trailing Stop aktiviert. |
| `CandleType` | Für alle Indikatoren verwendete Kerzenserie. Der Standardwert ist ein Zeitrahmen von 5 Minuten. |

## Implementierungshinweise

- Pip-Konvertierungen basieren auf dem Instrument `PriceStep`. Bei Instrumenten mit 3 oder 5 Dezimalstellen wird der Pip-Faktor mit zehn multipliziert, was den MetaTrader-Konventionen entspricht.
- Die Strategie verarbeitet nur fertige Kerzen, sodass die Ausführung nach dem Schließen jedes Balkens erfolgt.
- Interne Zustandsvariablen speichern die letzten Indizes für Retracement, EMA-Ausbruch und stochastische Bestätigungen, um die vom ursprünglichen Expertenberater verwendeten Rückblickfenster zu reproduzieren, ohne den gesamten Verlauf zu scannen.

## Nutzung

1. Hängen Sie die Strategie an eine `Connector`- oder `Trader`-Instanz mit einer konfigurierten Sicherheit und einem konfigurierten Portfolio an.
2. Stellen Sie sicher, dass das Wertpapier über einen gültigen `PriceStep` für die Pip-zu-Preis-Konvertierung verfügt.
3. Passen Sie die Parameter entsprechend der Instrumentenvolatilität an. Langsamer EMA ist standardmäßig auf 740 eingestellt, um mit der Quelle EA übereinzustimmen, aber schnellere Märkte können von kürzeren Einstellungen profitieren.
4. Starten Sie die Strategie. Markt- und Schutzaufträge werden automatisch generiert, wenn die oben beschriebenen Bedingungen erfüllt sind.

> **Haftungsausschluss**: Diese Strategie wurde für Bildungszwecke portiert. Vor dem Handel mit Live-Kapital werden gründliche Zukunftstests und Risikoanalysen empfohlen.
