# Überarbeitetes selbstadaptives EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Portierung des MetaTrader 5 Expertenberaters `revised_self_adaptive_ea.mq5` in das StockSharp High-Level-Strategie-Framework.

## Strategieüberblick

Der Algorithmus scannt eine konfigurierbare Kerzenreihe und sucht nach umfassenden Umkehrkonfigurationen, die durch Momentum- und Trendfilter bestätigt werden:

* **Mustererkennung** – bewertet die letzte geschlossene Kerze im Vergleich zur vorherigen. Ein bullisches Setup erfordert einen grünen Körper, der unter dem vorherigen Schlusskurs öffnet, während die vorherige Kerze bärisch ist. Bei bärischen Setups wird die Spiegellogik angewendet. Kerzenkörper werden mit einem gleitenden Durchschnitt verglichen, um schwache Signale herauszufiltern.
* **Momentum-Filter** – ein klassischer RSI stellt sicher, dass bullische Trades nur im überverkauften Bereich und bärische Trades im überkauften Bereich ausgelöst werden.
* **Trendfilter** – ein kurzer einfacher gleitender Durchschnitt muss mit der Handelsrichtung übereinstimmen. Dies verhindert, dass starke Trends ohne Bestätigung verblassen.
* **Risikomanagement** – ATR-gesteuerte Stop-Loss- und Take-Profit-Level werden für jede neue Position berechnet. Optionale Trailing-Stops verfolgen weiterhin profitable Bewegungen, ohne den Schutz zu verringern. Positionen werden zwangsweise geschlossen, wenn der Preis die Schutzniveaus erreicht.
* **Spread- und Risikoschutz** – Trades werden übersprungen, wenn der aktuelle Spread den konfigurierten Schwellenwert überschreitet oder wenn der auf ATR basierende Stop ein Risiko darstellen würde, das über dem zulässigen Prozentsatz des Preises liegt.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `CandleType` | Zur Analyse verwendete Kerzenaggregation. Standardmäßig werden einstündige Balken verwendet. |
| `AverageBodyPeriod` | Anzahl der Kerzen, die zur Berechnung des Filters für die durchschnittliche Körpergröße verwendet werden. |
| `MovingAveragePeriod` | Länge des einfachen gleitenden Durchschnitts, der als Richtungsfilter fungiert. |
| `RsiPeriod` | RSI Länge, die für die Überverkauft-/Überkauft-Bestätigung verwendet wird. |
| `OversoldLevel` | RSI Schwelle, die erreicht werden muss, bevor eine bullische Umkehr akzeptiert wird. |
| `OverboughtLevel` | RSI Schwelle, die erreicht werden muss, bevor eine rückläufige Umkehr akzeptiert wird. |
| `AtrPeriod` | ATR Länge, die für volatilitätsbasierte Schutzabstände verwendet wird. |
| `StopLossAtrMultiplier` | Auf ATR angewendeter Multiplikationsfaktor für die Stop-Loss-Distanz. |
| `TakeProfitAtrMultiplier` | Auf ATR angewendeter Multiplikationsfaktor für die Take-Profit-Distanz. |
| `TrailingStopAtrMultiplier` | ATR Abstand, der von der Trailing-Stop-Logik verwaltet wird. |
| `UseTrailingStop` | Aktiviert den Trailing-Stop-Supervisor. |
| `MaxSpreadPoints` | Maximal zulässiger Spread (ausgedrückt in Preisschritten/Pips). Signale werden ignoriert, wenn der Markt breiter ist. |
| `MaxRiskPercent` | Maximal akzeptables prozentuales Risiko basierend auf dem ATR-Stopp im Verhältnis zum Einstiegspreis. |
| `TradeVolume` | Basislosgröße, die für Marktaufträge verwendet wird. |

## Verhaltenshinweise

* Die Positionen werden abgeflacht, bevor die Richtung umgekehrt wird, um die MetaTrader-Implementierung widerzuspiegeln.
* Die Schutz-Stopp-/Take-Werte werden nach jeder Füllung anhand des letzten ATR-Werts neu berechnet.
* Der Trailing Stop bewegt sich nur in Handelsrichtung und ist deaktiviert, wenn noch keine ATR-Daten verfügbar sind.
* Wenn die Strategie auf einem Instrument ohne zuverlässige Geld-/Briefkurse ausgeführt wird, bleibt der Spread-Filter automatisch inaktiv.

## Unterschiede zum Original MQL

Das ursprüngliche Skript beschrieb lediglich die Signalerkennungsroutine. In diesem Port wurden die fehlenden Elemente mithilfe der bereitgestellten Parameter rekonstruiert:

* Bestätigung des gleitenden Durchschnitts hinzugefügt, um das in der MQL-Quelle deklarierte MA-Handle zu verwenden.
* Implementierte ATR-basierte Stop-Loss-, Take-Profit- und Trailing-Stop-Logik unter Verwendung des im ursprünglichen Experten definierten Volatilitäts-Handles.
* Es wurde ein Risikoprozentschutz hinzugefügt, sodass übergroße ATR-Stopps übersprungen werden, anstatt blind ausgeführt zu werden.
* Visualisierungselemente (Diagrammpfeile) wurden weggelassen, da StockSharp-Strategien standardmäßig keine Objekte in Diagrammen zeichnen.

## Nutzung

1. Hängen Sie die Strategie an ein Portfolio und eine Sicherheit in Hydra oder Ihrem benutzerdefinierten StockSharp-Host an.
2. Stellen Sie sicher, dass das Kerzenabonnement dem vorgesehenen Zeitrahmen entspricht (Standard: eine Stunde).
3. Passen Sie die Risikoparameter an, um die Volatilität des Instruments widerzuspiegeln.
4. Starten Sie die Strategie. Es abonniert automatisch Kerzen, berechnet Indikatoren und platziert Marktaufträge, wenn die Bedingungen erfüllt sind.
