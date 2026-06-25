# Exp BlauCMI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Strategie recreiert den MetaTrader 5-Expertenberater **Exp_BlauCMI** unter Verwendung der StockSharp-High-Level-API. Sie berechnet den Blau Candle Momentum Index (CMI), ein dreifach geglättetes Momentum-Verhältnis, auf einer konfigurierbaren Kerzenserie und reagiert auf Schwünge im Oszillator. Long-Trades werden eröffnet, wenn der Indikator nach einem Abschwung aufwärts dreht, Short-Trades wenn er nach einem Aufschwung abwärts dreht. Das Modul hält die Implementierung vollständig ereignisgesteuert – Orders werden nur nach dem Schließen von Kerzen gesendet.

## Indikatorlogik
1. Zwei Preisquellen werden über `Momentum Price` und `Reference Price` ausgewählt. Das Roh-Momentum ist die Differenz zwischen dem aktuellen Wert des ersten Preises und dem verzögerten Wert des zweiten Preises. Die Verzögerung wird durch `Momentum Depth` gesteuert.
2. Sowohl das Momentum als auch sein absoluter Wert werden durch drei aufeinanderfolgende gleitende Durchschnitte (`First/Second/Third Smoothing`) geleitet. Für jede Stufe wird dieselbe Mittelungsmethode verwendet und kann unter einfachen, exponentiellen, geglätteten (RMA) und linear gewichteten gleitenden Durchschnitten ausgewählt werden.
3. Der Blau CMI wird als `100 * smoothedMomentum / smoothedAbsMomentum` berechnet. Der Indikator beginnt Handelssignale zu erzeugen, sobald die dritte Glättungsstufe genug Bars angesammelt hat.
4. Der Parameter `Signal Shift` bestimmt, wie viele abgeschlossene Kerzen zurück die Strategie vor der Auswertung von Umkehrungen inspiziert (ein Wert von 1 reproduziert das ursprüngliche EA und verwendet den zuletzt geschlossenen Bar).

## Handelsregeln
- **Long-Einstieg** – erlaubt wenn `Allow Long Entry` aktiviert ist und die Indikatorsequenz `Value[Signal Shift - 1] < Value[Signal Shift - 2]` gefolgt von `Value[Signal Shift] > Value[Signal Shift - 1]` beobachtet wird, was bedeutet dass der Oszillator gerade aufwärts gedreht hat. Bestehende Short-Positionen werden zuerst geschlossen wenn `Allow Short Exit` aktiviert ist.
- **Short-Einstieg** – erlaubt wenn `Allow Short Entry` aktiviert ist und der Indikator abwärts dreht (`Value[Signal Shift - 1] > Value[Signal Shift - 2]` und `Value[Signal Shift] < Value[Signal Shift - 1]`). Bestehende Long-Positionen werden vorab geschlossen wenn `Allow Long Exit` aktiviert ist.
- **Long-Ausstieg** – wenn in einer Long-Position und die Short-Einstiegsbedingung auslöst, wird die Position geschlossen wenn `Allow Long Exit` true ist.
- **Short-Ausstieg** – wenn in einer Short-Position und die Long-Einstiegsbedingung auslöst, wird die Position geschlossen wenn `Allow Short Exit` true ist.
- Alle Trades werden mit Marktorders unter Verwendung des in `Order Volume` angegebenen Volumens ausgeführt. Protective Stop-Loss- und Take-Profit-Brackets werden automatisch über `StartProtection` angehängt und bleiben aktiv während die Position offen ist.

## Parameter
- `Candle Type` – Datentyp (Zeitrahmen oder andere Kerzenbeschreibung) für die Indikatorberechnung und Handelsentscheidungen. Standard sind 4-Stunden-Kerzen.
- `Smoothing Method` – Mittelungsalgorithmus für alle drei Glättungsstufen (Einfach, Exponentiell, Geglättet, Linear Gewichtet).
- `Momentum Depth` – Anzahl der Bars zwischen den zwei Preispunkten, die das Roh-Momentum bilden.
- `First/Second/Third Smoothing` – Längen der drei Mittelungsstufen, die sowohl auf das Momentum als auch auf seinen absoluten Wert angewendet werden.
- `Signal Shift` – Anzahl bereits abgeschlossener Kerzen, die bei der Auswertung von Umkehrmustern zurückgeschaut wird (Mindestwert ist 1).
- `Momentum Price` – angewendeter Preis für das nicht verzögerte Bein der Momentum-Berechnung.
- `Reference Price` – angewendeter Preis für das verzögerte Vergleichsbein.
- `Allow Long Entry`, `Allow Short Entry` – Schalter zum Erlauben von Einstiegen in jede Richtung.
- `Allow Long Exit`, `Allow Short Exit` – Schalter, die steuern ob entgegengesetzte Signale die jeweiligen Positionen schließen.
- `Stop-Loss Points`, `Take-Profit Points` – Risikolimits in Preisschritten (`Security.PriceStep`). Bei null wird das entsprechende Bracket deaktiviert.
- `Order Volume` – absolute Menge beim Senden von Marktorders. Die Strategie weist diesen Wert auch der Basis `Strategy.Volume`-Eigenschaft zu.

## Zusätzliche Hinweise
- Die unterstützten Glättungsmethoden entsprechen StockSharp-Indikatoren: Einfacher gleitender Durchschnitt, Exponentieller gleitender Durchschnitt, Geglätteter gleitender Durchschnitt (RMA) und Gewichteter gleitender Durchschnitt.
- Die Demark-Preiskonstante repliziert die MT5-Implementierung durch Mittelung der Preisextreme und des Kerzen-Schlusskurses vor dem Anpassen der High/Low-Abstände.
- Da Berechnungen nur abgeschlossene Kerzen verwenden, reagiert die Strategie einmal pro Bar und entspricht dem ursprünglichen EA-Verhalten, das auf neue Bars via `IsNewBar` prüfte.
- `Stop-Loss Points` und `Take-Profit Points` werden als Vielfache des Instrumentenpreisschritts interpretiert, um konsistent mit den punktbasierten Eingaben der ursprünglichen MQL5-Strategie zu bleiben.
