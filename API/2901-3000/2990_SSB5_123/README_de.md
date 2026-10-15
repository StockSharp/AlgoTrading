# SSB5_123 Multi-Indikator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein StockSharp-Port des MetaTrader 5 Expert Advisors "ssb5_123". Der ursprüngliche Code stammt aus der SSB-Kollektion (Step by Step) von Yury V. Reshetov und kombiniert mehrere klassische Oszillatoren zur Bestätigung von Richtungsausbrüchen. Die StockSharp-Version behält dieselbe Logik bei und verwendet die High-Level-Kerzenabonnement-API sowie native Indikatorimplementierungen.

Der Algorithmus arbeitet ausschließlich mit abgeschlossenen Kerzen. Er vergleicht den Eröffnungspreis der aktuellen Kerze mit der vorherigen, überprüft den Impuls und die Richtung des Awesome Oscillators, MACD und OsMA-Histogramms und verifiziert, dass der Preis über oder unter einem geglätteten gleitenden Durchschnitt gehandelt wird. Zusätzliche Bestätigung wird vom stochastischen Oszillator erhalten, indem gefordert wird, dass sowohl %K als auch %D über oder unter dem Level 50 liegen.

## Indikatoren und Signale
Die folgenden Indikatoren werden genau wie in der MetaTrader-Version eingesetzt:

- **Geglätteter gleitender Durchschnitt (SMMA)**: 45-Perioden-geglätteter gleitender Durchschnitt, der aus den Kerzenöffnungen berechnet wird. Die Einstiegsrichtung erfordert, dass der Eröffnungspreis auf der richtigen Seite des Durchschnitts liegt.
- **MACD (schnell 47, langsam 95, Signal 74)**: Die Hauptlinie muss für Long-Trades positiv (für Short-Trades negativ) sein und im Vergleich zur vorherigen Kerze steigen (fallen).
- **OsMA-Histogramm**: Berechnet als MACD minus seiner Signallinie. Das Histogramm muss für Long-Trades sinken und für Short-Trades steigen, was der ursprünglichen `fosma1()`-Funktion entspricht.
- **Awesome Oscillator**: Verwendet die standardmäßigen geglätteten 5/34-Durchschnitte des Medianpreises. Der Oszillatorwert muss für Longs positiv (für Shorts negativ) sein und sein Impuls zwischen den letzten zwei Balken muss in die Handelsrichtung zeigen.
- **Stochastischer Oszillator (K=25, D=12, Slowing=56)**: Sowohl die %K- als auch die %D-Linie müssen für Long-Trades über 50 und für Short-Trades unter 50 liegen, was einen Regime-Filter darstellt.

## Handelslogik
1. Auf eine neue abgeschlossene Kerze warten.
2. Das **Long-Setup** auswerten. Alle folgenden Bedingungen müssen wahr sein:
   - Die aktuelle Kerzenöffnung ist kleiner oder gleich der vorherigen Kerzenöffnung.
   - Awesome Oscillator ist positiv und fällt gegenüber dem vorherigen Wert.
   - Die MACD-Hauptlinie ist positiv und steigt gegenüber dem vorherigen Wert.
   - Das OsMA-Histogramm nimmt nicht zu (aktuelles Histogramm minus vorheriges Histogramm ist kleiner oder gleich null).
   - Die aktuelle Kerzenöffnung liegt über dem geglätteten gleitenden Durchschnitt.
   - Die stochastischen %K- und %D-Linien liegen bei oder über 50.
3. Das **Short-Setup** auswerten. Alle folgenden Bedingungen müssen wahr sein:
   - Die aktuelle Kerzenöffnung ist größer oder gleich der vorherigen Kerzenöffnung.
   - Awesome Oscillator ist negativ und steigt gegenüber dem vorherigen Wert.
   - Die MACD-Hauptlinie ist negativ und fällt gegenüber dem vorherigen Wert.
   - Das OsMA-Histogramm nimmt nicht ab (aktuelles Histogramm minus vorheriges Histogramm ist größer oder gleich null).
   - Die aktuelle Kerzenöffnung liegt unter dem geglätteten gleitenden Durchschnitt.
   - Die stochastischen %K- und %D-Linien liegen bei oder unter 50.
4. Wenn bereits eine Position besteht, schließt ein entgegengesetztes Signal diese sofort, was das ursprüngliche MetaTrader-Ordermanagement repliziert.
5. Bei flacher Position hat ein Long-Einstieg Priorität: Wenn beide Signale zufällig wahr sind (möglich, wenn alle Indikatoren genau null sind), öffnet die Strategie eine Long-Position. Andernfalls öffnet sie eine Short-Position, wenn nur die Short-Bedingungen erfüllt sind.

## Parameter
- **SMMA Period** – Länge des geglätteten gleitenden Durchschnittsfilters (Standard 45).
- **MACD Fast / Slow / Signal** – EMA-Perioden für den MACD-Indikator (47 / 95 / 74).
- **Stochastic %K / %D / Slowing** – Hauptperiode, Glättungsperiode und zusätzliche Verlangsamung für den stochastischen Oszillator (25 / 12 / 56).
- **Order Volume** – Menge für Marktaufträge (Standard 1).
- **Candle Type** – Zeitrahmen der Eingabekerzen (Standard 1 Stunde). Passen Sie dies an den in MetaTrader verwendeten Zeitrahmen an.

## Verwendungshinweise
- Die Strategie arbeitet nur mit abgeschlossenen Kerzen; Intrabar-Updates werden ignoriert.
- Indikatorwerte der vorherigen Kerze werden zwischengespeichert, damit die Impulsvergleiche genau dem Verhalten der ursprünglichen Hilfsfunktionen `fao1`, `fmacd1` und `fosma1` entsprechen.
- Im ursprünglichen Expert Advisor gibt es keine integrierten Stop-Loss- oder Take-Profit-Regeln. Das Risikomanagement sollte bei Bedarf extern hinzugefügt werden.
- Die Standard-Indikatoreinstellungen entsprechen den bereitgestellten MQL-Parametern, aber alle Werte werden als `StrategyParam`-Objekte exponiert und können über den StockSharp-Optimierer optimiert werden.

## Konvertierungshinweise
- Die MetaTrader-Version verwendet eine magische Zahl und manuelle Volumenvalidierung; diese Teile sind in StockSharp nicht erforderlich und wurden weggelassen.
- Die Auftragsschließungslogik folgt derselben Priorität wie das MQL-Skript: Positionen werden zuerst geschlossen, und neue Einstiege werden nur vorgenommen, wenn die Strategie flach ist.
- Die Awesome Oscillator- und MACD-Implementierungen stammen aus der StockSharp-Indikatorbibliothek, wodurch die im ursprünglichen Code vorhandene manuelle Pufferverarbeitung entfällt.
