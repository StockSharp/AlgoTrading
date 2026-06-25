# Slow Stochastic Mode-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Slow Stochastic Mode-Strategie** ist eine Konvertierung des MetaTrader-Expert-Advisors `Exp_Slow-Stoch.mq5` in die StockSharp-High-Level-API. Das System handelt auf dem Schlusskurs abgeschlossener Kerzen und verwendet einen geglätteten stochastischen Oszillator zur Erkennung von Regimewechseln. Es stehen drei verschiedene Signalmodi zur Verfügung, sodass der Trader entscheiden kann, ob er auf Levelbrüche, Momentum-Umschwünge oder Linienkreuzungen reagiert.

## Kernidee

Die Strategie beobachtet die %K- und %D-Linien eines langsamen stochastischen Oszillators, der durch den `Slowing`-Parameter zusätzlich geglättet wird. Abhängig vom ausgewählten *Signalmodus* wertet der Algorithmus den Oszillator eine oder mehrere Bars zurück aus (gesteuert durch `SignalBar`) und öffnet entweder eine neue Position oder schließt die Gegenseite, wenn ein qualifizierendes Ereignis erscheint. Aufträge werden immer als Marktausführungen platziert.

## Signalmodi

- **Breakdown** – sucht nach einem Durchbruch von %K durch das 50-Niveau. Ein Kreuz von unten nach oben durch 50 erzeugt einen Long-Einstieg und schließt Short-Positionen. Ein Kreuz von oben nach unten durch 50 erzeugt einen Short-Einstieg und schließt Long-Positionen.
- **Twist** – erkennt eine Richtungsänderung von %K. Wenn der Oszillator vor zwei Bars fiel und sich auf der ausgewerteten Bar nach oben dreht, öffnet oder dreht die Strategie in einen Long-Trade. Die umgekehrte Situation löst Shorts aus.
- **CloudTwist** – verfolgt den Farbwechsel der stochastischen "Cloud" durch Beobachtung eines %K vs %D-Kreuzes. Ein bullisches Kreuz (%K über %D) öffnet oder schützt Longs, während ein bärisches Kreuz (%K unter %D) das Gegenteil bewirkt.

Alle Modi respektieren die vier Berechtigungsschalter: Long/Short-Einstiege und Long/Short-Ausstiege können unabhängig aktiviert oder deaktiviert werden.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | H4-Zeitrahmen | Kerzentyp für Indikatorberechnungen. |
| `KPeriod` | 5 | Rückblickperiode für die %K-Linie. |
| `DPeriod` | 3 | Gleitende-Durchschnitt-Länge für %D. |
| `Slowing` | 3 | Zusätzliche Glättung auf %K vor Vergleichen. |
| `SignalBar` | 1 | Anzahl der geschlossenen Bars zurück zur Signalauswertung. |
| `StopLossPoints` | 1000 | Stop-Loss-Abstand in Instrument-Schritten (0 zum Deaktivieren). |
| `TakeProfitPoints` | 2000 | Take-Profit-Abstand in Instrument-Schritten (0 zum Deaktivieren). |
| `EnableLongEntries` | true | Erlaubt der Strategie, Long-Positionen zu öffnen. |
| `EnableShortEntries` | true | Erlaubt der Strategie, Short-Positionen zu öffnen. |
| `EnableLongExits` | true | Erlaubt das Schließen von Long-Positionen bei einem Umkehrsignal. |
| `EnableShortExits` | true | Erlaubt das Schließen von Short-Positionen bei einem Umkehrsignal. |
| `Mode` | Twist | Ausgewählter Signalmodus. |

Die Strategie verwendet den integrierten StockSharp-`StochasticOscillator`-Indikator und speist ihn mit den konfigurierten Längen. Der `SignalBar`-Parameter ermöglicht die Reproduktion des MetaTrader-Verhaltens der Referenzierung der vorherigen Kerze (Standard = 1) oder des Handelns auf der zuletzt abgeschlossenen Bar bei Einstellung auf 0.

## Handelsverwaltung

- Aufträge werden mit `BuyMarket`- und `SellMarket`-Aufrufen übermittelt. Positionswechsel werden automatisch durch Addition des absoluten Wertes der aktuellen Position zum Basis-Ordervolumen behandelt.
- Optionaler Stop-Loss- und Take-Profit-Schutz wird über `StartProtection` aktiviert. Abstände werden als Ticks/Schritte interpretiert, daher multipliziert StockSharp sie intern mit der Instrumentenschrittgröße.
- Kein Trailing Stop ist angehängt; der Schutz bleibt statisch bis zur Ausführung oder bis die Strategie manuell aussteigt.

## Ausstiegslogik

- Im **Breakdown**-Modus schließt derselbe Schwellenbruch, der eine Seite öffnet, die andere.
- Im **Twist**-Modus schließt das Erkennen einer Momentum-Umkehr die entgegengesetzte Position, bevor die neue eröffnet wird.
- Im **CloudTwist**-Modus löst das Kreuzen von %K und %D sowohl den Einstieg aus als auch liquidiert gleichzeitig die entgegengesetzte Seite.

Wenn Einstiegsberechtigungen deaktiviert sind, bleibt nur die entsprechende Ausstiegslogik aktiv, was Benutzern ermöglicht, die Strategie im "Bestehende Exposition verwalten"-Modus zu betreiben.

## Implementierungshinweise

- Die Oszillatorhistorie wird in kleinen In-Memory-Puffern verfolgt, damit die Strategie die vom ursprünglichen Expert-Advisor benötigten Bar-Offsets inspizieren kann.
- Alle Entscheidungen werden nur auf abgeschlossenen Kerzen ausgewertet (`candle.State == Finished`).
- Das Chart-Rendering zeichnet die zugrunde liegenden Kerzen und den stochastischen Oszillator, wenn Chart-Dienste verfügbar sind.

Diese Konvertierung behält die Absicht des ursprünglichen MQL5-Systems bei und nutzt gleichzeitig die Indikator-Bindings, Parameter-Metadaten und integrierten Risikokontrollen von StockSharp.
