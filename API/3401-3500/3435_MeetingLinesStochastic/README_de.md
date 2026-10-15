# Meeting Lines Stochastic Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Meeting Lines Stochastic-Strategie** ist eine StockSharp-Implementierung des MetaTrader-Experten *Expert_AML_Stoch*. Es kombiniert die Candlestick-Umkehrmuster der Bullish/Bearish Meeting Lines mit einer Bestätigung durch die %D-Signallinie des Stochastic-Oszillators. Die Strategie richtet sich an diskretionäre Händler, die einen regelbasierten Ansatz zur Mustererkennung mit zusätzlicher Momentum-Bestätigung wünschen. Durch die Verwendung des High-Level-Codes StockSharp API bleibt der Code prägnant, testbar und lässt sich für das Portfoliomanagement oder die weitere Automatisierung leicht erweitern.

## Handelslogik

1. **Candlestick-Musterfilter**
   - Die Strategie wertet kontinuierlich die letzten beiden abgeschlossenen Kerzen aus, um eine Meeting-Lines-Formation zu erkennen.
   - Ein bullisches Setup erfordert eine lange schwarze Kerze, gefolgt von einer langen weißen Kerze, deren Schlusskurs innerhalb von 10 % des vorherigen Schlusskurses liegt.
   - Ein bärisches Setup erfordert eine lange weiße Kerze, gefolgt von einer langen schwarzen Kerze mit der gleichen engen Ausrichtung von 10 %.
   - Die durchschnittliche Kerzenkörpergröße wird mit einem konfigurierbaren einfachen gleitenden Durchschnitt berechnet, um schwache Körper herauszufiltern.

2. **Stochastic Bestätigung**
   - Die %D-Signalleitung des Stochastic-Oszillators muss das Candlestick-Signal bestätigen.
   - Bullische Einstiege erfordern, dass %D unter dem konfigurierbaren Überverkaufsschwellenwert liegt (Standard 30).
   - Für bärische Einträge muss %D über dem konfigurierbaren Überkaufschwellenwert liegen (Standard 70).

3. **Ausgangsregeln**
   - Short-Positionen werden geschlossen, wenn %D entweder das untere Ausstiegsniveau (Standard 20) oder das obere Ausstiegsniveau (Standard 80) nach oben kreuzt.
   - Long-Positionen werden geschlossen, wenn %D die gleichen Niveaus nach unten kreuzt.
   - Umkehraufträge schließen automatisch bestehende Engagements und eröffnen eine neue Position in die entgegengesetzte Richtung.

4. **Volumenhandhabung**
   - Die Strategie verwendet die Basiseigenschaft `Volume`, wenn sie positiv ist; Andernfalls wird aus Kompatibilitätsgründen mit dem Verhalten von MetaTrader bei festen Chargen standardmäßig eine einzelne Charge verwendet.

## Parameter

| Name | Beschreibung | Standard | Notizen |
| ---- | ----------- | ------- | ----- |
| `CandleType` | Zur Analyse verwendete primäre Kerzenserie. | 15-minütiger Zeitrahmen | Akzeptiert alle `DataType`, die von StockSharp unterstützt werden. |
| `StochasticLength` | Lookback-Zeitraum für die %K-Rohberechnung. | 3 | Spiegelt den MetaTrader `%K period`. |
| `StochasticSmoothing` | Glättung angewendet auf %K (MetaTrader `slowing`). | 25 | Legt die interne Glättungslänge des Oszillators fest. |
| `StochasticSignal` | Glättungszeitraum für die %D-Signalleitung. | 36 | Spiegelt den MetaTrader `%D period`. |
| `BodyAveragePeriod` | Anzahl der Kerzen, anhand derer die Körpergröße der Kerze gemittelt wird. | 3 | Filtert kleinere Körper beim Erkennen von Begegnungslinien heraus. |
| `LongEntryLevel` | Maximaler %D-Wert, der immer noch einen bullischen Einstieg ermöglicht. | 30 | Entspricht dem Überverkaufsschwellenwert. |
| `ShortEntryLevel` | Mindestwert %D für einen rückläufigen Einstieg erforderlich. | 70 | Entspricht der Überkaufschwelle. |
| `ExitLowerLevel` | Untere Grenze, die Ausgänge bei Aufwärtskreuzungen auslöst. | 20 | Wird sowohl für Long- als auch für Short-Exit-Entscheidungen verwendet. |
| `ExitUpperLevel` | Obere Grenze, die Ausgänge bei Abwärtskreuzungen auslöst. | 80 | Wird sowohl für Long- als auch für Short-Exit-Entscheidungen verwendet. |

Alle Parameter werden über `StrategyParam<T>` verfügbar gemacht und können direkt im StockSharp Designer oder programmgesteuert optimiert werden.

## Signalerzeugung

- **Long-Einstieg**: Bullische Begegnungslinien + %D unter `LongEntryLevel` ohne bestehendes Long-Engagement (Short-Positionen sind umgekehrt).
- **Short-Einstieg**: Bearish Meeting Lines + %D über `ShortEntryLevel` ohne bestehendes Short-Engagement (Long-Positionen sind umgekehrt).
- **Langer Ausstieg**: %D unterschreitet `ExitUpperLevel` oder `ExitLowerLevel`.
- **Kurzer Ausstieg**: %D überschreitet `ExitLowerLevel` oder `ExitUpperLevel`.

## Implementierungshinweise

- Die Indikatordaten werden über `BindEx` verarbeitet, wodurch eine manuelle Verwaltung der Indikatorsammlung entfällt.
- Bei der Mittelung des Kerzenkörpers wird ein `SimpleMovingAverage` verwendet, der bis `DecimalIndicatorValue` mit absoluten Körpergrößen gefüttert wird und mit dem MetaTrader-Helfer `AvgBody` übereinstimmt.
- Alle Kommentare innerhalb des Codes sind in Englisch verfasst und die Einrückung basiert auf Tabulatorzeichen gemäß den Projektrichtlinien.
- Die Strategie zeichnet automatisch Kerzen und den stochastischen Oszillator, wenn ein Chartbereich verfügbar ist, was die Live-Überwachung vereinfacht.

## Nutzungstipps

1. **Optimierung**: Verwenden Sie die bereitgestellten Parameter für Walk-Forward-Tests, um die Schwellenwerte an das gehandelte Instrument anzupassen.
2. **Risikomanagement**: Layern Sie die Strategie mit den integrierten `StartProtection` von StockSharp oder externen Risikokontrollen auf Portfolioebene für Produktionsbereitstellungen.
3. **Datenqualität**: Meeting-Lines-Muster reagieren empfindlich auf genaue Eröffnungs-/Schlusskurse; Stellen Sie sicher, dass die Feeds ausgerichtet sind und illiquide Sitzungen gefiltert werden.
4. **Zeitrahmen**: Obwohl der Standardwert 15 Minuten beträgt, können Intraday- oder Tagesdaten durch Ändern von `CandleType` verwendet werden.

Die Strategie bietet einen disziplinierten Ansatz für Händler, die sich auf Candlestick-Formationen verlassen, aber eine Oszillatorbestätigung benötigen, um Fehlalarme zu reduzieren.
