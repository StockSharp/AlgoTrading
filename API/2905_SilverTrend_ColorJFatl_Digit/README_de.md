# SilverTrend ColorJFatl Digit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die SilverTrend ColorJFatl Digit-Strategie verbindet zwei klassische MetaTrader-Systeme zu einer einheitlichen High-Level-StockSharp-Strategie. Der SilverTrend-Block identifiziert Richtungsausbrüche, indem er misst, wie weit der Preis innerhalb eines kurzen Donchian-artigen Kanals reist. Der ColorJFatl Digit-Block glättet den Preis mit einem Jurik Moving Average (JMA) und bewertet seine Steigung, nachdem die Ausgabe auf die konfigurierte Anzahl von Stellen gerundet wurde. Nur wenn beide Subsysteme über die Richtung einig sind, öffnet oder hält die Strategie eine Position. Wenn die Signale divergieren, verlässt die Strategie zur Flat.

Das Design behält den Geist des ursprünglichen Expertenberaters bei und nutzt gleichzeitig die High-Level-API von StockSharp: Kerzenabonnements, Indikatorbindungen, warteschlangenbasierte Signalverzögerungen und Diagrammzeichnungshilfen. Jeder Schritt ist ausführlich dokumentiert, um weitere Forschung und Optimierung einfach zu machen.

## Strategielogik

### 1. SilverTrend-Ausbruchsdetektor

* Verwendet `Highest`- und `Lowest`-Indikatoren mit `SilverTrendLength + 1` Kerzen, um den aktuellen Preiskanal zu bilden.
* Der Kanal wird durch den `SilverTrendRisk`-Parameter verengt: je höher der Risikowert, desto näher liegen die Ausbruchsschwellen an der Kanalmittellinie (ursprüngliche Formel `33 - risk`).
* Wenn der Schluss über die angepasste obere Schwelle bricht, meldet der SilverTrend-Block einen bullischen Trend (`+1`). Wenn er unter die untere Schwelle bricht, meldet der Block einen bärischen Trend (`-1`).
* Eine konfigurierbare Verzögerung (`SilverTrendSignalBar`) wartet `n` vollständig geschlossene Kerzen, bevor das Signal als gültig gilt, und ahmt die MQL-`SignalBar`-Logik nach.

### 2. ColorJFatl Digit-Bestätigungsfilter

* Ein `JurikMovingAverage` glättet den durch `JmaPriceType` ausgewählten angewandten Preis. Alle MetaTrader-angewandten-Preis-Varianten werden unterstützt (Schluss, Eröffnung, Median, typisch, gewichtet, einfach, Viertel, Trendfolge-Modi und Demark-Berechnung).
* Die Jurik-Ausgabe wird auf `JmaRoundDigits` gerundet und reproduziert das diskretisierte "Digit"-Indikatorverhalten.
* Das Steigungszeichen des gerundeten JMA wird zum Trendsignal. Wenn die Steigung positiv ist, gibt der Filter `+1` aus; wenn negativ, `-1`. Flache Steigungen erben den vorherigen Zustand, um ruckartige Umschaltungen zu vermeiden.
* Wie bei SilverTrend verzögert `JmaSignalBar` die Ausführung und erfordert, dass die Steigung für die angeforderte Anzahl geschlossener Kerzen anhält.

### 3. Trade-Ausführung

* **Einstieg:**
  * Long gehen, wenn sowohl SilverTrend als auch ColorJFatl `+1` melden und keine bestehende Long-Exposition vorhanden ist.
  * Short gehen, wenn beide Blöcke `-1` melden und keine bestehende Short-Exposition vorhanden ist.
* **Ausstieg:**
  * Die aktuelle Position sofort schließen, wenn die Signale divergieren (z.B. ein Block sagt `+1`, der andere `-1` oder `0`).
  * Umkehrungen schließen automatisch die gegenläufige Exposition, bevor die neue Position eröffnet wird, um Mittelwertbildung zu vermeiden.
* Aktive Aufträge werden vor Umkehrungen storniert, um das Buch sauber zu halten.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `SilverTrendCandleType` | Kerzenserie für die Berechnung des SilverTrend-Ausbruchskanals. Standardmäßig H4-Äquivalent. |
| `SilverTrendLength` | Rückblicklänge für die Kanalberechnung (`SSP`-Parameter im ursprünglichen EA). |
| `SilverTrendRisk` | Risikomodifikator, der die Ausbruchsschwellen verengt (`33 - risk`). Höhere Werte reagieren schneller, aber mit mehr Fehlsignalen. |
| `SilverTrendSignalBar` | Anzahl vollständig geschlossener Kerzen zum Warten, bevor eine SilverTrend-Farbänderung akzeptiert wird. |
| `ColorJfatlCandleType` | Kerzenserie für den Jurik-Filter. Kann sich vom SilverTrend-Zeitrahmen unterscheiden. |
| `JmaLength` | Länge des Jurik Moving Average. |
| `JmaSignalBar` | Verzögerung (in Balken) vor dem Handeln bei Jurik-Steigungswechseln. |
| `JmaPriceType` | Angewandter Preismodus für den Jurik-Eingang (Schluss, Eröffnung, Median, Trendfolge-Varianten, Demark usw.). |
| `JmaRoundDigits` | Anzahl der Dezimalstellen beim Runden der Jurik-Ausgabe, emuliert den digitalisierten Indikator. |

## Implementierungshinweise

* Signalverzögerungen werden mit kleinen FIFO-Warteschlangen statt großen historischen Arrays implementiert, um sicherzustellen, dass die Strategie speichereffizient und dem ursprünglichen Expert Advisor treu bleibt.
* Der Code fragt Indikatorbuffer nie direkt ab. Stattdessen bindet er Indikatoren über die High-Level-`SubscribeCandles().Bind(...)`-API, gemäß den Richtlinien in `AGENTS.md`.
* Englische Inline-Kommentare erklären jede Entscheidung: wann Schwellen neu berechnet werden, wie Steigungen berechnet werden, warum Aufträge storniert werden und wie Konsens durchgesetzt wird.
* Diagrammunterstützung ist enthalten: wenn ein Diagramm verfügbar ist, zeichnet die Strategie Preiskerzen, SilverTrend-Kanalllinien und eigene Trades, um Live-Entscheidungen zu visualisieren.

## Verwendungstipps

1. **Märkte und Zeitrahmen:** Das ursprüngliche System wurde für H4-Forex-Charts entwickelt. Krypto- und Rohstoff-Futures mit klarem Swing-Verhalten funktionieren ebenfalls gut. Für schnellere Märkte `SilverTrendLength` und `JmaLength` vorsichtig reduzieren.
2. **Optimierung:** Sowohl die Ausbruchlänge (`SilverTrendLength`) als auch die Bestätigungslänge (`JmaLength`) gemeinsam optimieren — das Kürzen nur eines Beins erzeugt meist widersprüchliche Signale.
3. **Angewandter-Preis-Experimente:** Die Trendfolge-Preismodi ausprobieren, wenn mit Heikin-Ashi- oder Renko-Feeds gearbeitet wird; sie glätten Rauschen oft besser als reine Schlusspreise.
4. **Risikokontrolle:** Die eingebauten Ausstiege mit portfolio-weiten Stops kombinieren. Da beide Module leicht verzögern, können Volatilitätsspitzen immer noch über den Kanal hinaus reichen, bevor der Filter wechselt.
5. **Positionsgröße:** Die Strategie überlässt die Volumenverwaltung der Basis-`Strategy.Volume`-Eigenschaft. Es anpassen oder StockSharp-Geldverwaltungserweiterungen integrieren, wenn Pyramidisierung oder Skalierung erforderlich ist.

## Weitere Forschungsideen

* ATR-basierte Stop-Loss- und Take-Profit-Schutzmaßnahmen durch `StartProtection` hinzufügen, sobald Tests die bevorzugten Schwellen bestätigen.
* Kerzen eines höheren Zeitrahmens (z.B. Täglich) in die Jurik-Bestätigung einspeisen, während SilverTrend auf H4 bleibt, um einen Trendfilter einzuführen.
* Mit volumenbasierten Filtern (On-Balance Volume, VWAP-Divergenz) für zusätzliche Bestätigung vor Einstiegen kombinieren.
