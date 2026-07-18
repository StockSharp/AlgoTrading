# Fibo Pivot MultiVal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Fibo Pivot MultiVal Strategy** ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `_Fibo_Pivot_multiVal.mq4`. Die
Die Strategie kombiniert tägliche Pivot-Punkte mit Fibonacci Retracement- und Extension-Verhältnissen, um Limit-Orders innerhalb jedes Preises einzusetzen
Zone, die den Drehpunkt umgibt. Handelssitzungen, Positionsziele und Halteregeln folgen daher dem ursprünglichen Expertenberater
Risikokontrolle und Ausführungsverhalten sind Händlern, die die MetaTrader-Version verwendet haben, weiterhin vertraut.

## Kernlogik

1. **Tägliche Referenzniveaus** werden aus den Höchst-, Tiefst- und Schlusskursen des Vortages berechnet. Klassische Pivot-Ebenen (P, R1-R3, S1-S3)
werden von Fibonacci-basierten internen Ebenen begleitet, die den Abstand zwischen dem Drehpunkt und der benachbarten Stütze aufteilen oder
Widerstandslinien. Zusätzliche R3/S3-Erweiterungen prognostizieren potenzielle Breakout-Ziele.
2. **Intraday-Preisbewegungen** werden im konfigurierten Kerzenzeitrahmen (standardmäßig 15 Minuten) überwacht. Wenn der Strom schließt
Liegt der Wert innerhalb einer bestimmten Pivot-Zone (z. B. zwischen R2 und R3), aktiviert die Strategie die entsprechenden Limit-Orders.
3. **Limit-Orders** werden auf den Unterebenen Fibonacci platziert. Jede Zone verwaltet sowohl Long- als auch Short-Orders mit der entsprechenden Richtung
gefiltert durch den Parameter `MidZoneOrderMode`, wenn der Preis zwischen R1-R2 und S1-S2 schwankt.
4. **Ziele** passen sich der Marktvolatilität an. Wenn `UseReversalTargets` aktiviert ist, befinden sich die Ausgänge auf der gegenüberliegenden Seite des aktiven
Fibonacci-Band zum Erfassen von Mean-Reversion-Bounces. Wenn die Option deaktiviert ist, vergleicht der Algorithmus den Bereich des Vortages mit dem
`LimitPointOut` und `LimitPointIn` Schwellenwerte, um zu entscheiden, ob längere Ausbrüche (in Richtung R3/S3-Erweiterungen) angestrebt werden sollen oder
tiefere Umkehrungen (in Richtung Pivot).
5. **Risikolimits** pausieren neue Trades, sobald die konfigurierbaren täglichen oder pro-Symbol-Gewinn-/Trade-Schwellenwerte überschritten werden. Alles ausstehend
Aufträge werden storniert und der Handel wird beim nächsten Zurücksetzen der Sitzung (vor `StartTime`) wieder aufgenommen.
6. **Sitzungsverwaltung** spiegelt das ursprüngliche EA wider: Der Handel beginnt bei `StartTime`, neue Einträge enden nach `FinishTime` und so weiter
Die offene Belichtung wird nach `CloseAllTime` abgeflacht.

## Parameter

| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `CandleType` | 15-Minuten-Kerzen | Zeitrahmen, der zum Erstellen der Entscheidungskerzen verwendet wird. |
| `OrderVolume` | `0.1` | Volumen für jede von der Strategie registrierte Limit-Order. |
| `StartTime` | `00:01` | Sitzungszeit, die den Handel ermöglicht und Zähler zurücksetzt. |
| `FinishTime` | `08:00` | Sitzungszeit, die neue Einträge deaktiviert, während bestehende Positionen beibehalten werden. |
| `CloseAllTime` | `12:00` | Sitzungszeit, die Aufträge storniert und alle Positionen schließt. |
| `UseReversalTargets` | `true` | Bei „true“ bleiben die Ziele innerhalb der Zone Fibonacci. Bei „Falsch“ werden Breakout-/Pivot-Ziele basierend auf der Tagesspanne verwendet. |
| `LimitPointIn` | `150` | Täglicher Bereichsschwellenwert (Punkte), der bei Überschreitung Pivot-Reversion-Ziele erzwingt. |
| `LimitPointOut` | `50` | Täglicher Bereichsschwellenwert (Punkte), der Ausbruchsziele fördert, wenn die Preisbewegung komprimiert ist. |
| `LevelPf1` | `33` | Prozentsatz, der zur Aufteilung der Pivot-R1- und Pivot-S1-Distanz verwendet wird. |
| `LevelF1F2` | `50` | Prozentsatz, der zur Berechnung des Zwischenniveaus zwischen R1–R2 und S1–S2 verwendet wird. |
| `LevelF2F3` | `33` | Prozentsatz, der zur Berechnung des Zwischenniveaus zwischen R2–R3 und S2–S3 verwendet wird. |
| `LevelF3Out` | `40` | Prozentsatz, der zur Verlängerung von R3/S3 für Breakout-Ziele verwendet wird. |
| `MidZoneOrderMode` | `"bs"` | Zulässige Wegbeschreibungen innerhalb der mittleren Zonen (`"b"`=nur kaufen, `"s"`=nur verkaufen, `"bs"`=beides). |
| `DailyProfitTarget` | `50` | Tägliches Gewinnlimit in Punkten. |
| `DailyTradeTarget` | `35` | Maximale Anzahl abgeschlossener Trades pro Tag. |
| `SymbolProfitTarget` | `150` | Gewinnziel pro Symbol in Punkten. |
| `SymbolTradeTarget` | `15` | Maximal abgeschlossene Trades pro Symbol und Tag. |

## Orderverwaltung

* Jede aktive Zone behält ihre eigenen Einstiegs-, Take-Profit- und optionalen Stop-Orders. Wenn ein Eintrag ausgeführt wird, werden Ausstiegsaufträge ausgeführt
neu erstellt unter Verwendung der Ziel-/Stoppniveaus, die aus der Fibonacci-Konfiguration abgeleitet wurden.
* Gefüllte Exits aktualisieren die täglichen und pro-Symbol-Statistiken. Beim Erreichen eines beliebigen Limits wird der Handel bis zum nächsten Zurücksetzen unterbrochen.
* Sitzungsgrenzen heben Eintrittsbefehle automatisch auf. Die `CloseAllTime`-Grenze schließt zusätzlich alle offenen Positionen über
Marktaufträge.

## Praktische Tipps

* Die Strategie erwartet Instrumente mit klar definierten Preisschritten. Stellen Sie sicher, dass die `Security`-Instanz `PriceStep` verfügbar macht, damit die
Die Point-to-Price-Konvertierung entspricht dem ursprünglichen EA.
* Passen Sie für Vermögenswerte mit unterschiedlichen Volatilitätsmerkmalen `LimitPointIn` und `LimitPointOut` so an, dass Ausbruch vs.
Mean-Reversion-Verhalten wird in geeigneten Bereichen ausgelöst.
* Wenn Sie direktionale Trades rund um die Mittelzone (R1-R2 oder S1-S2) bevorzugen, setzen Sie `MidZoneOrderMode` auf `"b"` oder `"s"`, um nur zuzulassen
lange oder kurze Setups.
* Nutzen Sie die integrierte Parameteroptimierungsunterstützung, um alternative Fibonacci-Verhältnisse zu testen. Alle prozentualen Parameter und
Schwellenwerte machen `SetCanOptimize` im Quellcode verfügbar und ermöglichen so automatisierte Scans im StockSharp Designer.

## Unterschiede zum ursprünglichen Expert Advisor

* Die Version StockSharp funktioniert mit einer einzelnen Sicherheit pro Strategieinstanz. Um mehrere Symbole wie im MetaTrader EA zu handeln,
Führen Sie für jedes Instrument separate Strategieinstanzen aus.
* Die Positionsgröße wird direkt in Volumeneinheiten und nicht in MetaTrader Lots ausgedrückt. Konfigurieren Sie `OrderVolume` passend zu Ihrem
Anforderungen des Maklers.
* Die Auftragsausführung basiert auf der hohen Ebene StockSharp API (`BuyLimit`, `SellLimit` usw.). Brokerspezifisches Verhalten (z. B
(Offsets für ausstehende Orders) sollten vor der Bereitstellung in der Produktion überprüft werden.
