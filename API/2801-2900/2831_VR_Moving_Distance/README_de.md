# VR Moving Distance-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese StockSharp-Strategie repliziert den VR-Moving Expert Advisor aus MetaTrader 5. Sie beobachtet einen konfigurierbaren gleitenden Durchschnitt und reagiert, wenn der Preis um eine feste Pip-Distanz abweicht. Der Algorithmus kann Trends durch Multiplikation des Basisordervolumens für Folgehandel skalieren und wendet einfache Take-Profit-Logik an, solange nur eine Position offen ist.

## Überblick
- Handelt das der Strategie zugewiesene Instrument mit einer einzigen Kerzendatensubskription.
- Berechnet einen gleitenden Durchschnitt mit wählbarer Länge, Glättungstyp und Preisquelle.
- Konvertiert Distanz- und Take-Profit-Einstellungen von Pips in Preisabweichungen mithilfe des Preisschritts des Wertpapiers.
- Fügt Long-Positionen hinzu, wenn der Preis weit genug über dem gleitenden Durchschnitt steigt, oder Short-Positionen, wenn der Preis darunter fällt.
- Kehrt die aktuelle Nettoexposition vor dem Öffnen einer Position in die entgegengesetzte Richtung um, um die Strategie netting-freundlich zu halten.

## Indikatoren und Daten
- Ein gleitender Durchschnitt (`Simple`, `Exponential`, `Smoothed`, `Weighted` oder `VolumeWeighted`).
- Kerzen kommen mit dem konfigurierten `Candle Type`; derselbe Datenstrom treibt Indikatorwerte und Handelsentscheidungen.

## Einstiegslogik
1. Bei jeder abgeschlossenen Kerze wartet die Strategie, bis der gleitende Durchschnitt vollständig gebildet ist.
2. Wenn das Hoch der Kerze mindestens `DistancePips` über dem gleitenden Durchschnitt liegt, wird ein Long-Einstieg ausgelöst.
3. Wenn das Tief der Kerze mindestens `DistancePips` unter dem gleitenden Durchschnitt liegt, wird ein Short-Einstieg ausgelöst.
4. Beim Richtungswechsel schließt die Strategie die bestehende Exposition, indem sie das entgegengesetzte Volumen zur neuen Marktorder hinzufügt.

## Skalierung und Volumenverwaltung
- Die erste Order verwendet das konfigurierte `BaseVolume`.
- Folgeorders in dieselbe Richtung verwenden `BaseVolume * VolumeMultiplier`.
- Der höchste ausgeführte Preis auf der Long-Seite und der niedrigste auf der Short-Seite werden verfolgt. Jede neue Skalierungsorder erfordert, dass der Preis sich um weitere `DistancePips` von diesem Extrem entfernt, bevor sie ausgelöst wird.

## Ausstiegslogik
- Wenn genau eine Long-Position offen ist, wird ein Gewinnziel beim Einstiegspreis plus `TakeProfitPips` (in Preiseinheiten umgerechnet) platziert. Wenn das Hoch einer Kerze das Ziel berührt, wird die Position geschlossen.
- Ebenso erhält eine einzelne Short-Position ein Gewinnziel bei Einstieg minus `TakeProfitPips` und wird geschlossen, wenn das Kerzentief es berührt.
- Sobald mehrere Einstiege existieren, hält die Strategie die Positionen offen und wartet auf neue Skalierungssignale; kein gemittelter Ausstieg wird in diesem Port versucht.

## Hinweise zum Risikomanagement
- `StartProtection()` wird beim Start aktiviert, um sich in die Standard-StockSharp-Schutzsysteme einzuklinken.
- Distanz- und Take-Profit-Werte werden in Pips gemessen. Für Symbole, die mit 3 oder 5 Dezimalstellen notiert werden, multipliziert die Strategie den Preisschritt mit 10, um der MetaTrader-Pip-Semantik zu entsprechen.
- Es gibt keinen automatischen Stop-Loss; das Risiko muss durch die gewählten Parameter und externe Portfoliolimits kontrolliert werden.

## Parameter
- **Candle Type** – Datentyp für die Kerzensubskription.
- **MA Length** – Periode des gleitenden Durchschnitts.
- **MA Type** – Glättungsmethode des gleitenden Durchschnitts.
- **Price Source** – Kerzenkurs zur Berechnung des gleitenden Durchschnitts.
- **Distance (pips)** – Minimale Pip-Lücke zwischen Preis und gleitendem Durchschnitt zum Auslösen von Einstiegen.
- **Take Profit (pips)** – Gewinnziel-Distanz, die angewendet wird, wenn nur eine Position offen ist.
- **Volume Multiplier** – Multiplikator auf das Basisvolumen für zusätzliche Einstiege.
- **Base Volume** – Menge des ersten Handels.
