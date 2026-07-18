# FX-CHAOS Scalping-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die FX-CHAOS Scalping-Strategie repliziert den MT5-Expert Advisor, der den Awesome Oscillator mit fraktalbasierten ZigZag-Niveaus auf mehreren Zeitrahmen kombiniert. Der StockSharp-Port abonniert stündliche Kerzen für die Handelsausführung und tägliche Kerzen für einen übergeordneten Zeitrahmenfilter. Interne Tracker rekonstruieren die "ZigZag on Fractals"-Logik durch Erkennung von Fünf-Kerzen-Fraktalmustern und verbinden diese zu alternierenden Swing-Punkten.

## Handels-Workflow
1. **Datenabruf**
   - Stündliche Kerzen steuern Einstiege und Risikomanagement.
   - Tägliche Kerzen versorgen den übergeordneten ZigZag-Filter.
   - Ein Awesome Oscillator (5, 34) wird auf dem stündlichen Feed berechnet.
2. **Fraktaler ZigZag-Tracking**
   - Jede abgeschlossene Kerze wird in ein Fünf-Element-Schiebefenster eingespeist.
   - Wenn die mittlere Kerze ein Auf-/Ab-Fraktal bildet, wird der letzte Swing-Wert aktualisiert; aufeinanderfolgende Swings in dieselbe Richtung werden nur durch extremere Werte ersetzt.
3. **Signalerfassung beim stündlichen Schluss**
   - Ein Long-Signal erscheint, wenn die Kerze unterhalb des vorherigen Hochs öffnet, darüber schließt, unterhalb des letzten stündlichen ZigZag-Swings bleibt, über dem letzten täglichen ZigZag-Niveau liegt und der Awesome Oscillator negativ ist.
   - Ein Short-Signal spiegelt die Logik unter Verwendung des vorherigen Tiefs und der entgegengesetzten Oszillatorpolarität.
4. **Orderausführung**
   - Bestehende entgegengesetzte Positionen werden geschlossen, bevor ein neuer Einstieg mit dem konfigurierten Volumen platziert wird.
   - Der Einstiegspreis wird für das nachfolgende Stop-Loss- und Take-Profit-Management gespeichert.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `Volume` | Handelsvolumen in Lots. Wird auf jede Market Order angewendet. |
| `Stop Loss (pts)` | Abstand in Punkten für den Schutz-Stop. Der Wert wird mit dem Kursschritt des Instruments multipliziert. Auf `0` setzen zum Deaktivieren. |
| `Take Profit (pts)` | Abstand in Punkten für das Gewinnziel. Auf dieselbe Weise mit dem Kursschritt konvertiert. Auf `0` setzen zum Deaktivieren. |
| `Trading Candle` | Primärer Zeitrahmen für Einstiege (standardmäßig 1 Stunde). |
| `Daily Candle` | Übergeordneter Zeitrahmen für den ZigZag-Filter (standardmäßig 1 Tag). |

## Risikomanagement
- Bei jeder abgeschlossenen stündlichen Kerze prüft die Strategie, ob der Preis das Stop-Loss- oder Take-Profit-Niveau erreicht hat, das vom gespeicherten Einstiegspreis abgeleitet wurde.
- Eine ausgeführte Schutzorder schließt die Position sofort und setzt das Einstiegspreisflag zurück, um einen Wiedereinstieg im selben Kerzenzyklus zu verhindern.
- Positionen werden auch geschlossen, wenn ein neues Signal in die entgegengesetzte Richtung erscheint.

## Implementierungshinweise
- Die benutzerdefinierte ZigZag-Logik vermeidet direkte Indikatorpuffer und folgt den Repository-Richtlinien, indem sie auf Kerzenabonnements mit minimalem lokalem Zustand arbeitet.
- ZigZag-Werte bleiben `null`, bis genügend Kerzen verarbeitet wurden (zwei Balken auf jeder Seite eines potenziellen Fraktals). Der Handel wird ausgesetzt, bis sowohl stündliche als auch tägliche Tracker gültige Swings erzeugen.
- Der Awesome Oscillator wird über `BindEx` angefordert, was sicherstellt, dass die Strategie nur finale Indikatorwerte verwendet, wenn alle Eingaben bereit sind.
- Preisabstände werden mit `Security.PriceStep` skaliert. Falls das Instrument keinen Schritt hat, verwendet die Strategie einen Ein-Punkt-Multiplikator.

## Dateien
- `CS/FxChaosScalpStrategy.cs` – Strategieimplementierung mit ZigZag-Tracker, Awesome Oscillator-Filter und Orderlogik.
- `README_zh.md` – Dokumentation auf Vereinfachtem Chinesisch.
- `README_ru.md` – Dokumentation auf Russisch.
