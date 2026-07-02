# Gandalf PRO-Projektionsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Gandalf PRO-Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters *Gandalf_PRO*. Der ursprüngliche Roboter baut einen
adaptiver Glättungsfilter aus einem gewichteten gleitenden Durchschnitt und einer rekursiven Trendkomponente. Wenn sich der prognostizierte Preis bewegt
Mindestens 15 Pips über dem aktuellen Marktpreis, steigt der EA mit einem entfernten Stop-Loss und einem Take-Profit in diese Richtung
projiziertes Niveau. Die StockSharp-Konvertierung reproduziert denselben Filter und dieselbe Entscheidungslogik und stützt sich dabei auf die Kerze auf hoher Ebene
API, sodass jede Berechnung an fertigen Stäben durchgeführt wird.

## Handelslogik
1. Abonnieren Sie den von `CandleType` ausgewählten Zeitrahmen (Standard: 1-Stunden-Kerzen) und verarbeiten Sie nur abgeschlossene Kerzen.
2. Führen Sie eine fortlaufende Historie der Schlusskurse, die groß genug ist, um das Maximum von `CountBuy` und `CountSell` plus einen zusätzlichen Balken abzudecken.
3. Erstellen Sie die Funktion MetaTrader `Out()` neu: Berechnen Sie linear gewichtete und einfache gleitende Durchschnitte (unter Verwendung einer Verschiebung um einen Balken) und leiten Sie die ab
rekursive `s`- und `t`-Komponenten mit den konfigurierten Preis- und Trendfaktoren und erhalten Sie den prognostizierten Preis `s[1] + t[1]`.
4. Für lange Setups (`EnableBuy`):
   - Überprüfen Sie, ob der prognostizierte Preis mindestens `15` Pips über dem letzten Schlusskurs liegt (`Bid + 15*x*Point` in MT4).
   - Wenn keine Long-Position offen ist, kaufen Sie das konfigurierte Volumen (siehe `BaseVolume` und `BuyRiskMultiplier`).
   - Speichern Sie den prognostizierten Preis als Take-Profit und berechnen Sie den Stop-Loss, indem Sie `BuyStopLossPips` umgerechnet in Preisschritte subtrahieren.
5. Für kurze Setups (`EnableSell`):
   - Der prognostizierte Preis muss mindestens `15` Pips unter dem letzten Schlusskurs liegen.
   - Wenn keine Short-Position offen ist, verkaufen Sie das konfigurierte Volumen (gegebenenfalls unter Umkehr einer bestehenden Long-Position).
   - Speichern Sie den prognostizierten Preis als Take-Profit und legen Sie den Stop-Loss `SellStopLossPips` Pips über dem Markt fest.
6. Während eine Position existiert, überwachen Sie jede fertige Kerze:
   - Verlassen Sie Long-Positionen, wenn das Tief der Kerze den gespeicherten Stop kreuzt oder das Hoch den Take-Profit erreicht.
   - Verlassen Sie Short-Positionen, wenn das Hoch der Kerze den Stopp überschreitet oder das Tief das Ziel erreicht.
   - Exits verwenden `ClosePosition()`, wodurch die Nettopräsenz in StockSharp abgeflacht wird.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `EnableBuy` | `bool` | `true` | Erlauben Sie der Strategie, Long-Positionen zu eröffnen. |
| `CountBuy` | `int` | `24` | Länge des Glättungsfilters, der für lange Projektionen verwendet wird. |
| `BuyPriceFactor` | `decimal` | `0.18` | Gewicht des aktuellen Abschlusses im langen rekursiven Filter. |
| `BuyTrendFactor` | `decimal` | `0.18` | Gewichtung, die beim Erstellen der langen Projektion auf den Trendterm angewendet wird. |
| `BuyStopLossPips` | `int` | `62` | Stop-Loss-Distanz für Long-Positionen, gemessen in Pips. |
| `BuyRiskMultiplier` | `decimal` | `0` | Der Multiplikator wird auf `BaseVolume` angewendet, bevor eine Langbestellung gesendet wird (0 behält das Basisvolumen bei). |
| `EnableSell` | `bool` | `true` | Erlauben Sie der Strategie, Short-Positionen zu eröffnen. |
| `CountSell` | `int` | `24` | Länge des Glättungsfilters, der für kurze Projektionen verwendet wird. |
| `SellPriceFactor` | `decimal` | `0.18` | Gewicht des aktuellen Abschlusses im kurzen rekursiven Filter. |
| `SellTrendFactor` | `decimal` | `0.18` | Gewichtung, die beim Erstellen der Kurzprojektion auf den Trendterm angewendet wird. |
| `SellStopLossPips` | `int` | `62` | Stop-Loss-Distanz für Short-Positionen, gemessen in Pips. |
| `SellRiskMultiplier` | `decimal` | `0` | Der Multiplikator wird auf `BaseVolume` angewendet, bevor eine Kurzbestellung gesendet wird (0 behält das Basisvolumen bei). |
| `BaseVolume` | `decimal` | `1` | Basisauftragsgröße, die verwendet wird, wenn beide Risikomultiplikatoren Null sind. |
| `CandleType` | `DataType` | 1-stündiger Zeitrahmen | Von der Strategie verarbeitete Kerzenserie. |

## Unterschiede zum Original MetaTrader EA
- MetaTrader kann gleichzeitig unabhängige Kauf- und Verkaufstickets halten. StockSharp verwendet Nettopositionen, sodass der Port geschlossen wird oder
kehrt eine bestehende Position um, bevor die gegenüberliegende Seite geöffnet wird.
- Die MT4-Lotfunktion nutzte die kontofreie Marge. Die Konvertierung legt `BaseVolume` und zwei Risikomultiplikatoren offen; wenn sie Null sind
Das Basisvolumen wird unverändert verwendet, andernfalls wird das Volumen einfach skaliert (`BaseVolume * RiskMultiplier`).
- Stop-Loss- und Take-Profit-Level werden durch die Überwachung abgeschlossener Kerzen erreicht. Intrabar-Füllungen können daher von MetaTrader abweichen.
bei denen Schutzanordnungen vom Makler verwaltet werden.
- Die fünfstellige `Digits`/`Point`-Anpassung wird durch Überprüfung von `Security.Decimals` und `Security.PriceStep` emuliert, um Pip umzurechnen
Distanzen in absolute Preise umwandeln.
- Alle Indikatorberechnungen werden in verwaltetem Code durchgeführt, ohne dass `iMA` aufgerufen wird. Der rekursive Filter wird in neu erstellt
`CalculateTarget` unter Verwendung derselben Koeffizienten wie die Funktion MQL.

## Nutzungshinweise
- Weisen Sie `Strategy.Security` das gewünschte Instrument zu, bevor Sie beginnen. Die Strategie löst eine Ausnahme aus, wenn keine Sicherheit angehängt ist.
- Konfigurieren Sie `BaseVolume` so, dass es der von Ihrem Veranstaltungsort erwarteten Vertragsgröße entspricht. Passen Sie die Risikomultiplikatoren nur an, wenn Sie skalieren möchten
die Belichtung relativ zum Basisvolumen.
- Der Kerzenverlauf muss mindestens `max(CountBuy, CountSell) + 1` Balken enthalten, bevor ein Handel generiert werden kann. Ausreichend bereitstellen
Aufwärmdaten oder starten Sie die Strategie mit geladenen historischen Kerzen.
- Der 15-Pip-Eingabepuffer ist fest (genau wie im EA). Erhöhen Sie `CountBuy`/`CountSell`, um die Projektion zu glätten oder zu optimieren
Preis-/Trendfaktoren, die dem in MetaTrader beobachteten Verhalten entsprechen.
- Da Exits von den Extremwerten der Kerze abhängen, aktivieren Sie einen Zeitrahmen, der zu Ihrer Ausführungslatenz passt. Niedrigere Zeitrahmen werden früher reagieren
erfordern jedoch mehr historische Daten und können möglicherweise mehr Signale erzeugen.

## Details zur Implementierung
- Verwendet `SubscribeCandles()` mit `Bind(ProcessCandle)`, sodass jede Entscheidung auf endgültigen Kerzen basiert.
- Behält eine kompakte Liste der letzten Abschlüsse und erstellt den rekursiven `s`/`t`-Filter bei Bedarf neu, wodurch die `Out()`-Routine nachgeahmt wird.
- Konvertiert Pip-basierte Offsets über die Tick-Größe des Instruments und die Dezimalgenauigkeit, um die Skalierung MetaTrader `x * Point` zu reproduzieren.
- `ClosePosition()` wird aufgerufen, wenn Schutzniveaus durchbrochen werden, um sicherzustellen, dass die Nettoposition abgeflacht wird, bevor es zu einem weiteren Eintrag kommt
berücksichtigt.
