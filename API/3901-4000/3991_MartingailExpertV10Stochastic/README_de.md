# MartingailExpert v1.0 Stochastic Strategie (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **MartingailExpert v1.0 Stochastic-Strategie** ist eine direkte Konvertierung des MetaTrader 4 Expert Advisors
`MartingailExpert_v1_0_Stochastic.mq4`. Die Strategie überwacht die %K/%D-Linien des Stochastic-Oszillators
und eröffnet eine Position, wenn der zuvor abgeschlossene Balken oben eine Momentum-Bestätigung erzeugt (für Long-Positionen).
oder unterhalb (für Kurzschlüsse) konfigurierbarer Schwellenwerte. Sobald der erste Trade live ist, erstellt der Algorithmus einen
Martingalleiter zusätzlicher Marktaufträge, deren Volumen geometrisch wächst und deren gemeinsamer Take-Profit
bleibt an den Preis der letzten Ergänzung gebunden.

Die Konvertierung basiert vollständig auf dem übergeordneten API von StockSharp: Kerzenabonnements, Indikatorbindung und
integrierte `BuyMarket`/`SellMarket`-Helfer. Alle Codekommentare wurden in Englisch neu geschrieben und die Implementierung
folgt dem tabulatorbasierten Einrückungsstil, der in den Projektrichtlinien gefordert wird.

## Handelslogik

### 1. Einfahrtssignal

1. Der Stochastic-Oszillator (`Length = KPeriod`, `%K` Glättung = `Slowing`, `%D` Glättung = `DPeriod`) ist
an das Hauptkerzenabonnement gebunden. Es werden nur fertige Kerzen verarbeitet.
2. Die Strategie ahmt den ursprünglichen MQL-Aufruf `iStochastic(..., shift = 1)` nach, indem sie die vorherigen Balkenwerte speichert
von %K und %D. Ein langer Eintrag wird ausgelöst, wenn `K_prev > D_prev` und `D_prev > ZoneBuy`. Ein kurzer Eintrag ist
ausgelöst, wenn `K_prev < D_prev` und `D_prev < ZoneSell`.
3. Der allererste Handel verwendet `BuyVolume` oder `SellVolume` und setzt alle zu vermeidenden Gegenrichtungszustände zurück
Mischen von langen und kurzen Leitern.

### 2. Martingale Mittelung

1. Immer wenn ein offener Cluster vorhanden ist (`_buyOrderCount` oder `_sellOrderCount` größer als Null), gilt die Strategie
Überwacht das Tief (für Long-Positionen) oder das Hoch (für Short-Positionen) der Kerze.
2. **Schrittberechnung**
   * `StepMode = 0`: Die nächste Addition wartet darauf, dass sich der Preis um genau `StepPoints × PointSize` dagegen bewegt
die zuletzt ausgeführte Bestellung.
   * `StepMode = 1`: Die Entfernung wird zu `StepPoints + max(0, 2 × ordersCount − 2)` Punkten, entsprechend der
MQL Ausdruck `step + OrdersTotal*2 - 2`. Der Ausdruck wird mit der Punktgröße des Instruments multipliziert
(abgeleitet von `Security.PriceStep` und angepasst an 3/5-Dezimal-Devisenkurse).
3. Wenn die Kerze das Auslöseniveau überschreitet, sendet die Strategie sofort eine Marktorder mit dem gleichen Volumen
`previousVolume × Multiplier`. Die Lautstärken werden auf den `VolumeStep` des Instruments normalisiert, begrenzt durch
`VolumeMax` (sofern verfügbar) und auf Null abgerundet, wenn sie unter `VolumeMin` fallen.
4. Nach jeder Hinzufügung wird der gemeinsame Zielpreis auf aktualisiert
`lastEntryPrice ± ProfitFactorPoints × PointSize × orderCount` je nach Richtung.

### 3. Take-Profit-Management

1. Der Cluster wird geschlossen, sobald die Kerze den gemeinsamen Zielpreis berührt (`High >= target` für Long-Positionen,
`Low <= target` für Kurzfilme). Eine zusätzliche Prüfung schätzt den Preis-Distanz-Gewinn anhand der Gewichtung
durchschnittlicher Einstiegspreis, der den ursprünglichen `OrderProfit()`-Schutz von MQL widerspiegelt.
2. Alle offenen Bestellungen werden mit einem einzigen `SellMarket(Math.Abs(Position))` oder zusammengefasst
`BuyMarket(Math.Abs(Position))` Anruf. Nach einem erfolgreichen Exit wird der interne Martingale-Zustand zurückgesetzt.
3. Wenn das äußere Umfeld Positionen schließt (manueller Eingriff, Stop-Outs), erfolgt die nächste Kerze mit
`Position == 0` löscht automatisch den zwischengespeicherten Martingal-Status und sorgt so dafür, dass die Strategie konsistent bleibt.

### 4. Zusätzliche Implementierungshinweise

* Die Punktgröße wird von `Security.PriceStep` abgeleitet. Für 3- oder 5-dezimale FX-Symbole wird der Wert multipliziert
um zehn, um das MetaTrader-Konzept eines Pip (`Point`) zu emulieren.
* `StartProtection()` wird einmal in `OnStarted` aufgerufen, damit die Plattform allgemeine Schutzverhaltensweisen anhängen kann
(Timeouts, Heartbeat usw.).
* Die Strategie zeichnet zur Vereinfachung Kerzen, den stochastischen Indikator und eigene Trades in einem speziellen Diagrammbereich auf
Sichtprüfung bei Backtests.

## Parameter

| Name | Typ | Standard | Beschreibung |
| ---- | ---- | ------- | ----------- |
| `StepPoints` | dezimal | `25` | Entfernung in Punkten, bevor ein weiterer Martingalbefehl erteilt wird. |
| `StepMode` | int | `0` | `0` – feste Distanz, `1` – feste plus `2 × ordersCount − 2` Punkte. |
| `ProfitFactorPoints` | dezimal | `10` | Punkte, die pro offener Order addiert (oder subtrahiert) werden, um den Cluster-Take-Profit zu berechnen. |
| `Multiplier` | dezimal | `1.5` | Multiplikator, der für die nächste Addition auf das letzte Auftragsvolumen angewendet wird. |
| `BuyVolume` | dezimal | `0.01` | Volumen der anfänglichen Langbestellung. |
| `SellVolume` | dezimal | `0.01` | Volumen der ersten Short-Order. |
| `KPeriod` | int | `200` | Rückblickperiode des stochastischen Oszillators. |
| `DPeriod` | int | `20` | Glättungszeitraum für die %D-Signalleitung. |
| `Slowing` | int | `20` | Zusätzliche Glättung wurde auf %K (MetaTraders `slowing`) angewendet. |
| `ZoneBuy` | dezimal | `50` | Mindestwert %D erforderlich, um lange Einträge zu ermöglichen. |
| `ZoneSell` | dezimal | `50` | Maximaler %D-Wert erforderlich, um kurze Einträge zu ermöglichen. |
| `CandleType` | `DataType` | `5m time frame` | Kerzentyp, der für alle Indikatorberechnungen verwendet wird. |

## Ordnerstruktur

„
API/3991/
├── CS/
│ └── MartingailExpertV10StochasticStrategy.cs
├── README.md
├── README_zh.md
└── README_ru.md
„

Gemäß den Aufgabenanforderungen wird bewusst auf eine Python-Implementierung verzichtet.
