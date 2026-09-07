# Strategiediagramm für Kanalkreuzungen mit P&L-Schutz
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm berechnet aus abgeschlossenen Fünf-Minuten-Kerzen von BTCUSDT@BNBFT die Mitte eines 24-Kerzen-Kanals und handelt exakte Kreuzungen von Schlusskurs und Mitte nach einer Pause von 200 Kerzen. Die Markttiefe von TONUSDT@BNBFT bestätigt die Datenbereitschaft und taktet Prüfungen des nicht realisierten P&L; ein einmaliger Geldwertschutz fordert die Stornierung noch aktiver Einstiegsorders an und schließt die BTC-Position, sobald einer der eingestellten Schwellenwerte erreicht ist.

![schema](schema.svg)

## Strategieübersicht

- Die Variable BTC Security konfiguriert das Abonnement abgeschlossener Fünf-Minuten-Kerzen. Marktorders, Ausführungen, Positionsschließung und P&L gehören zur gewählten Strategy Security, die passend zu BTC Security auf BTCUSDT@BNBFT gesetzt werden muss.
- Highest(24) erhält BTC-Kerzen und verfolgt deren Hochs, während Lowest(24) deren Tiefs verfolgt. Die arithmetische Mitte ist `(Highest + Lowest) / 2`; die erste verfügbare Entscheidung speichert Schlusskurs und Mitte, startet die erste Pause und sendet keine Order.
- Eine Aufwärtskreuzung verlangt `Previous Close <= Previous Midpoint` und `Current Close > Current Midpoint`. Eine Abwärtskreuzung verlangt `Previous Close >= Previous Midpoint` und `Current Close < Current Midpoint`. Die gespeicherten Werte werden mit jeder abgeschlossenen BTC-Kerze fortgeschrieben, auch wenn die Pause eine Aktion ablehnt.
- Ein Einstieg oder eine Umkehr ist erst nach 200 strikt nachfolgenden abgeschlossenen BTC-Kerzen und mindestens einem TON-Markttiefenereignis zulässig. Jede akzeptierte Kreuzung startet die Verzögerung von 200 Kerzen neu, bevor ihre Marktorder gesendet wird.
- Ein durch Ausführungen aktualisierter vorzeichenbehafteter Merker hält den verwalteten BTC-Zustand: `-1` bedeutet Short, `0` ohne Position und `1` Long. Ein nicht realisierter P&L von mindestens `500` oder höchstens `-300` löst den einmaligen Schutz aus; eine spätere Kauf- oder Verkaufsausführung schaltet ihn wieder scharf.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn die exakte Aufwärtskreuzung auftritt, der vorzeichenbehaftete Merker ohne Position oder Short ist, beide Bereitschaftstore offen sind und die Pause beendet ist, wird eine Market-Kauforder gesendet. Der Einstieg ohne Position verwendet Base BTC Volume; die Umkehr von Short zu Long verwendet die doppelte Menge.
- **Short-Einstieg**: Wenn die exakte Abwärtskreuzung auftritt, der vorzeichenbehaftete Merker ohne Position oder Long ist, beide Bereitschaftstore offen sind und die Pause beendet ist, wird eine Market-Verkaufsorder gesendet. Der Einstieg ohne Position verwendet Base BTC Volume; die Umkehr von Long zu Short verwendet die doppelte Menge.
- **Ausstieg**: Eine zulässige Gegenkreuzung führt eine gewöhnliche Umkehr in einem Schritt statt einer getrennten Schließung aus. Unabhängig davon löst der P&L-Schutz bei `P&L >= Profit Target` oder `P&L <= -abs(Maximum Loss)` aus, sendet eine Massenstornierungsabsicht sowie gezielte Stornierungsanforderungen für die gespeicherten Einstiegsorders und fordert eine Marktschließung an. Diese geldwertbasierte Schließung startet die Kreuzungspause nicht neu.

## Parameter

| Parameter | Standardwert | Beschreibung |
|---|---|---|
| BTC Security | BTCUSDT@BNBFT | Instrument des Fünf-Minuten-Kerzenabonnements. Setzen Sie Strategy Security auf denselben Wert, da Orderaktionen, Ausführungen, Positionsschließung und P&L Strategy Security verwenden. |
| TON Readiness Security | TONUSDT@BNBFT | Instrument nur für das Markttiefenabonnement, das das Datenbereitschaftstor öffnet und die P&L-Abtastung taktet; seine Kurse werden weder für BTC-Orders noch für die P&L-Bewertung verwendet. |
| Candle Series | 00:05:00 | Abgeschlossene Fünf-Minuten-Kerzenserie von BTCUSDT für Kanal, exakte Kreuzungsentscheidungen, Pausenzählung und Diagramm. |
| Highest Length | 24 | Anzahl der BTC-Kerzen, aus denen Highest die obere Kanalgrenze berechnet. |
| Lowest Length | 24 | Anzahl der BTC-Kerzen, aus denen Lowest die untere Kanalgrenze berechnet. |
| Base BTC Volume | 1 | Standardmenge für Markteinstiege. Die Aktionsformel ist `Base BTC Volume * (1 + abs(latch))`; daher nutzt ein Einstieg ohne Position die Basismenge und eine Umkehr deren Doppeltes. |
| Cooldown N | 200 | Anzahl strikt nachfolgender abgeschlossener BTC-Kerzen, die nach der Initialisierung oder einer akzeptierten Kreuzung erforderlich sind, bevor eine weitere Kreuzung eine Order senden kann. |
| Profit Target | 500 | Nicht realisierter P&L-Wert, bei dessen Erreichen oder Überschreiten der einmalige Schutz Stornierung und Marktschließung anfordert. |
| Maximum Loss | 300 | Positiver Verlustbetrag; die Schutzschwelle wird als `-abs(Maximum Loss)` berechnet und beträgt standardmäßig `-300`. |

## Diagrammdetails

- Die BTC-[Variable](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) speist nur das Abonnement abgeschlossener [Kerzen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html). Die TON-Variable speist nur die [Markttiefe](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/market_depths/order_book.html); ihr erstes Ereignis setzt den Bereitschaftsmerker, spätere Ereignisse takten auch den letzten nicht realisierten P&L-Wert.
- Zwei [Indikator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Blöcke erhalten jede BTC-Kerze. Highest(24), Lowest(24), eine Mittenformel sowie Merker für aktuelle und vorherige Werte bewahren pro abgeschlossener Kerze genau eine vollständige Schlusskurs-und-Kanal-Entscheidung.
- Ein Delay-Block startet bei der ersten Entscheidung und wird bei jeder akzeptierten Kreuzung neu gestartet. Da die aktuelle Kerze seinen Eingang erreicht, bevor der Entscheidungszweig läuft, kehrt die Zulässigkeit erst nach 200 späteren abgeschlossenen BTC-Kerzen zurück; abgelehnte Kreuzungen ersetzen trotzdem den gespeicherten Schlusskurs und die Mitte.
- Kauf- und Verkaufsblöcke zur [Orderregistrierung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/orders/register.html) senden Marktorders mit `Base BTC Volume * (1 + abs(latch))`. Ihre MyTrade-Ausgänge schreiben den vorzeichenbehafteten Zustand und schalten den P&L-Schutz anhand tatsächlicher Ausführungen wieder scharf.
- P&L-Änderungsereignisse und TON-Markttiefenereignisse tasten den letzten nicht realisierten P&L ab. Strikte Schwellenvergleiche speisen ein gemeinsames scharfgeschaltetes Tor, sodass das Erreichen von `500` oder `-300` bis zu einer späteren Einstiegsausführung nur eine Schutzaktion erzeugen kann.
- Der Schutz ruft die [Massenstornierung von Orders](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html) auf, gibt die gespeicherten Kauf- und Verkauf-Orderreferenzen an gezielte Stornierungsblöcke weiter und schließt das aktuelle BTC-Engagement mit Base BTC Volume am Markt. Das Diagramm zeigt BTC-Kerzen, Highest(24), Lowest(24), Mitte, P&L, gesendete Orders und alle Strategieausführungen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, setzen Sie Strategy Security auf BTCUSDT@BNBFT, führen Sie sie im Backtester mit Kerzen- und Markttiefenhistorie aus und passen Sie danach Parameter oder Blöcke an Ihr Instrument an, bevor Sie live handeln.
