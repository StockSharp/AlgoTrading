# MACD PatternTrader Sitzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist eine direkte Konvertierung des MetaTrader-Expertenberaters **MacdPatternTraderAll0.01**. Sie handelt ein einzelnes Instrument mit sechs verschiedenen MACD-basierten Einstiegsmustern, optionaler Handelszeit-Filterung, Teilgewinnmitnahme und einer Martingale-Positionsgrößenoption. Alle Berechnungen werden auf abgeschlossenen Kerzen durchgeführt, die vom konfigurierten `CandleType` geliefert werden.

## Handelslogik
1. Bei jeder abgeschlossenen Kerze aktualisiert die Strategie sechs MACD-Indikatoren (jedes Muster hat seine eigenen schnellen und langsamen EMA-Längen und eine einperiodige Signallinie).
2. Wenn die Handelszeitfilterung aktiviert ist, werden neue Trades nur zwischen `SessionStart` und `SessionEnd` bewertet. Das Risikomanagement ist immer aktiv.
3. Jedes MACD-Muster prüft sehr spezifische Wertbeziehungen zwischen dem aktuellen MACD-Wert und den zwei vorherigen Werten, um Momentum-Umkehrungen zu erkennen. Wenn ein Muster ausgelöst wird, sendet es eine Marktorder in der entsprechenden Richtung und setzt interne Stop-Loss- und Take-Profit-Niveaus.
4. Der Stop-Loss wird als jüngstes Extrem (höchstes Hoch für Shorts, niedrigstes Tief für Longs) eines konfigurierbaren Lookbacks plus/minus einem in Preisschritten gemessenen Offset berechnet. Der Take-Profit scannt ältere Kerzengruppen in Blöcken, um die rekursive Zielsuche des ursprünglichen Experten zu replizieren.
5. Es wird immer nur eine Nettoposition verwaltet. Wenn ein neues Signal in entgegengesetzter Richtung erscheint, wird die aktuelle Position geschlossen und eine umgekehrte Position mit dem Martingale-angepassten Volumen geöffnet.
6. Aktive Positionen werden durch `ManageActivePosition` überwacht. Die Logik emuliert die ursprüngliche Teilschließungsroutine:
   - Bei Longs: wenn der Gewinn `ProfitThreshold` (5 Währungseinheiten) übersteigt und der vorherige Schlusskurs über dem mittelfristigen EMA liegt, wird ein Drittel der Position verkauft. Wenn der Gewinn anhält und das vorherige Hoch über dem Durchschnitt aus langem SMA und sehr langsamem EMA liegt, wird die Hälfte der verbleibenden Position geschlossen.
   - Bei Shorts: symmetrische Regeln schließen ein Drittel und dann die Hälfte der verbleibenden Position, wenn Gewinnziele und gleitende Durchschnittsfilter erfüllt sind.
7. Das Risikomanagement läuft bei jeder Kerze unabhängig vom Handelsfenster. Wenn der Preis das gespeicherte Stop-Loss- oder Take-Profit-Niveau innerhalb einer Kerze durchbricht (basierend auf Hoch/Tief), wird die gesamte Position zum Durchbruchpreis abgeflacht.
8. Nachdem ein Trade vollständig geschlossen wurde, wird das realisierte PnL ausgewertet. Wenn `UseMartingale` aktiviert ist, verdoppelt ein verlusttragender Trade das nächste Ordervolumen, während jeder profitable Ausstieg das Volumen auf den Basis-`LotSize` zurücksetzt.

## Schlüsselmuster
- **Muster 1:** Erkennt MACD-Spitzen über `Pattern1MaxThreshold`, die sich nach unten wenden, und Einbrüche unter `Pattern1MinThreshold`, die aufprallen.
- **Muster 2:** Sucht MACD-Kreuzungen um die Nulllinie mit minimalen Ausschlägen.
- **Muster 3:** Verwendet zweistufige Schwellen (`Pattern3MaxThreshold`, `Pattern3SecondaryMax`, `Pattern3MinThreshold`, `Pattern3SecondaryMin`), um Drei-Schritt-Umkehrungen auf beiden Seiten zu erkennen. Zählt auch aufeinanderfolgende Balken über dem sekundären Maximum, um die ursprüngliche `bars_bup`-Akkumulation nachzuahmen.
- **Muster 4:** Handelt, wenn MACD die primären Schwellen überschreitet, aber der vorherige Balken im engeren sekundären Bereich liegt, und Umkehrungen antizipiert.
- **Muster 5:** Reagiert auf schnelle MACD-Wendemanöver innerhalb enger Bereiche, die durch `Pattern5PrimaryMax/Min` und sekundäre Grenzen definiert sind.
- **Muster 6:** Verwendet Zähler (`Pattern6MaxBars`, `Pattern6MinBars`, `Pattern6CountBars`), um mehrere aufeinanderfolgende MACD-Ausschläge zu fordern, bevor ein Trade ausgelöst wird.

## Risikomanagement
- Interne Stop-Loss- und Take-Profit-Ziele werden für jeden Einstieg neu berechnet. Stops verwenden Preisextreme plus einen Offset in Preisschritten. Der Take-Profit durchsucht aufeinanderfolgende Kerzenblöcke, bis ein Extrem nicht mehr verbessert wird, und reproduziert damit die rekursive Logik des MQL-Experten.
- Teilausstiege respektieren die ursprüngliche Mindestlosgröße (0.01) und verfolgen, wie viele Teilschließungen pro Richtung ausgeführt wurden.
- Die Strategie platziert keine brokerseitigen Schutzorders; stattdessen überwacht sie die Kerzenhochs und -tiefs, um Positionen zu den konfigurierten Preisen zu schließen.

## Parameter
| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Kerzenserie für Indikatoren und Handelssignale. | 1-Stunden-Kerzen |
| `LotSize` | Basis-Tradingvolumen vor Martingale-Anpassungen. | 0.1 |
| `UseTimeFilter` | Handel nur zwischen `SessionStart` und `SessionEnd` erlauben. | true |
| `SessionStart` / `SessionEnd` | Handelsfenster (lokale Börsenzeit). | 07:00 / 17:00 |
| `UseMartingale` | `LotSize` nach einem verlusttragenden Trade verdoppeln. | true |
| `Ema1Period`, `Ema2Period`, `SmaPeriod`, `Ema3Period` | Gleitende Durchschnitte für Teilausstiege. | 7, 21, 98, 365 |
| Musterspezifische Parameter | Jedes Muster hat seine eigene Aktivierungsmarkierung, Stop-Loss/Take-Profit-Lookbacks, Offsets, EMA-Längen und Schwellenwerte entsprechend dem ursprünglichen Expertenberater. | Siehe Konstruktor-Standards |

Alle Schwellen und EMA-Längen sind über `StrategyParam`-Objekte zugänglich und ermöglichen Optimierung oder Feinabstimmung.

## Hinweise
- Die Strategie geht davon aus, dass das Instrument `PriceStep` und `PriceStepCost` bereitstellt, um Offsets und Gewinne in Kontowährung zu übersetzen. Wenn nicht verfügbar, werden Preisdifferenzen direkt verwendet.
- Stops und Ziele werden intern simuliert; sie werden beim Balken-Schließen ausgewertet. Die Intrabar-Ausführung in Echtzeit kann sich vom MetaTrader-Verhalten unterscheiden.
- Der Martingale-Mechanismus kann das Exposure nach einer Verluststrähne schnell erhöhen—mit Vorsicht verwenden.
