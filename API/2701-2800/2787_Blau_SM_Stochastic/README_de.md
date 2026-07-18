# Blau SM Stochastic Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine C#-Konvertierung des ursprünglichen MetaTrader 5-Experten `Exp_BlauSMStochastic`, der um den Blau SM Stochastic Oszillator aufgebaut ist. Der Indikator misst den Abstand zwischen Preis und dem letzten Handelsspanne, wendet mehrere Glättungsstufen an und vergleicht das Ergebnis mit einer geglätteten Referenzlinie. Die Strategie arbeitet auf abgeschlossenen Kerzen (Standard 4-Stunden-Zeitrahmen) und erlaubt den Handel in beide Richtungen.

## Indikatorlogik
1. Berechnung des höchsten Hochs und niedrigsten Tiefs über `LookbackLength` Balken.
2. Erstellen einer trendbereinigten Preisserie: `sm = price - (HH + LL) / 2` wobei `price` der angewendete Preistyp ist.
3. Glätten der trendbereinigten Serie sequentiell durch drei gleitende Durchschnitte mit Längen `FirstSmoothingLength`, `SecondSmoothingLength` und `ThirdSmoothingLength` unter Verwendung der gewählten `SmoothMethod` (SMA, EMA, SMMA oder LWMA).
4. Glätten des Halbbereichs `(HH - LL) / 2` mit derselben dreifachen Sequenz zur Normalisierung der Volatilität.
5. Bildung der Hauptoszillatorlinie als `100 * smoothed(sm) / smoothed(range)`.
6. Glätten der Hauptlinie mit `SignalLength` zur Gewinnung der Signallinie.

Der Parameter `Phase` wird aus Kompatibilitätsgründen mit der MQL-Version beibehalten, wird jedoch nicht von der vereinfachten Glättungsmaschine verwendet.

## Handelsmodi
- **Breakdown**: Überwacht Nulldurchgänge der Hauptlinie. Ein Übergang von positiv zu nicht-positiv eröffnet Long und schließt Shorts. Ein Übergang von negativ zu nicht-negativ eröffnet Short und schließt Longs.
- **Twist**: Verfolgt Momentumwenden. Wenn die Hauptlinie einen lokalen Tiefpunkt bildet (Wert steigt nach dem Fallen), wird ein Long-Einstieg ausgelöst, während ein lokaler Hochpunkt (Wert fällt nach dem Steigen) einen Short auslöst. Entgegengesetzte Positionen werden entsprechend geschlossen.
- **CloudTwist**: Beobachtet Kreuzungen zwischen der Hauptlinie und der Signallinie. Ein Abwärtsdurchgang der Hauptlinie durch die Signallinie öffnet Long und beendet Shorts, während ein Aufwärtsdurchgang Short öffnet und Longs beendet.

Einstiegs- und Ausstiegsschalter (`EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, `EnableShortExit`) ermöglichen das Deaktivieren bestimmter Operationen, während Indikatorberechnungen intakt bleiben.

## Risikomanagement
`TakeProfitPoints` und `StopLossPoints` werden in absolute Preisabstände unter Verwendung des Instrumentenpreisschritts konvertiert und über `StartProtection` an den eingebauten Schutzblock übergeben. Setzen Sie sie auf null, um das entsprechende Limit zu deaktivieren.

## Parameter
- `CandleType` *(DataType, Standard: 4-Stunden-Zeitrahmen)* – Zeitrahmen für Kerzenabonnement und Indikatorberechnungen.
- `Mode` *(BlauSmStochasticModes, Standard: Twist)* – wählt den Signalgenerierungsmodus (Breakdown, Twist, CloudTwist).
- `SignalBar` *(int, Standard: 1)* – Anzahl der Balken zum Verschieben von Indikatorwerten bei der Signalbewertung, repliziert die ursprüngliche `SignalBar`-Logik.
- `LookbackLength` *(int, Standard: 5)* – Balken zur Berechnung der höchsten und niedrigsten Werte.
- `FirstSmoothingLength` *(int, Standard: 20)* – Länge der ersten Glättungsstufe.
- `SecondSmoothingLength` *(int, Standard: 5)* – Länge der zweiten Glättungsstufe.
- `ThirdSmoothingLength` *(int, Standard: 3)* – Länge der dritten Glättungsstufe.
- `SignalLength` *(int, Standard: 3)* – Glättungslänge der Signallinie.
- `SmoothMethod` *(BlauSmSmoothMethods, Standard: EMA)* – Gleitender-Durchschnitt-Familie für alle Glättungsstufen (SMA, EMA, SMMA, LWMA).
- `PriceType` *(BlauSmAppliedPrices, Standard: Close)* – angewendeter Preis für den Oszillator (Schluss, Eröffnung, Hoch, Tief, Median, Typisch, Gewichtet, Einfach, Quartil, Trendfolgevarianten, Demark).
- `EnableLongEntry` *(bool, Standard: true)* – Erlaubt das Öffnen von Long-Positionen.
- `EnableShortEntry` *(bool, Standard: true)* – Erlaubt das Öffnen von Short-Positionen.
- `EnableLongExit` *(bool, Standard: true)* – Erlaubt das Schließen von Long-Positionen.
- `EnableShortExit` *(bool, Standard: true)* – Erlaubt das Schließen von Short-Positionen.
- `TakeProfitPoints` *(int, Standard: 2000)* – Fester Take-Profit-Abstand ausgedrückt in Instrumentenpunkten.
- `StopLossPoints` *(int, Standard: 1000)* – Fester Stop-Loss-Abstand ausgedrückt in Instrumentenpunkten.

## Hinweise
- Die Glättungsmaschine unterstützt derzeit klassische gleitende Durchschnitte (SMA, EMA, SMMA, LWMA). Exotische Modi aus der MQL-Bibliothek (JMA, JurX, etc.) sind in StockSharp nicht verfügbar und daher nicht enthalten.
- Phase wird als Parameter zur Vollständigkeit beibehalten; passen Sie es nur für Dokumentationszwecke an.
- Funktioniert mit jedem von StockSharp unterstützten Symbol. Passen Sie Kerzentyp, Glättungslängen und Stops an die Instrumentvolatilität an.
