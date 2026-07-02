# Harter Gewinn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Hard Profit ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `hardprofit.mq4`. Die Strategie versucht, Ausbrüche zu erfassen
nach einer Erschöpfungsbewegung, wenn der Schlusskurs am äußersten Ende der Kerze endet und ein geglätteter Trendfilter die Richtung bestätigt.
Der Port stellt die ursprünglichen Geldverwaltungsmodi, die gestaffelte Gewinnmitnahme und die Stoppverwaltung unter Verwendung von StockSharp wieder her
hochrangiges API.

## Strategielogik
### Breakout-Setup
* Die Strategie überwacht abgeschlossene Kerzen aus dem konfigurierten Zeitrahmen und verfolgt das höchste Hoch und das niedrigste Tief des
vorherige `Breakout Period`-Balken (die aktuelle Kerze ist ausgeschlossen, wodurch der Aufruf `iHighest`/`iLowest` mit einer Verschiebung von 1 emuliert wird).
* Medianpreise liefern einen geglätteten gleitenden Durchschnitt mit der Periode `Trend Period`. Die Steigung des gleitenden Durchschnitts (aktueller Wert minus
vorheriger Wert) ist der Richtungsfilter, der vom ursprünglichen EA verwendet wird.

### Einreisebestimmungen
* **Lange Einträge** werden berücksichtigt, wenn:
  * Die Kerze schließt auf ihrem Höchststand und durchbricht das vorherige Bereichshoch.
  * Die geglättete Steigung des gleitenden Durchschnitts ist positiv.
  * Es gibt keine offene Position und das Trade-pro-Bar-Limit wurde nicht erreicht.
  * Der aktuelle Spread (bester Brief minus bestes Geld) liegt unter dem Schwellenwert `Max Spread (pips)`, wenn beide Seiten verfügbar sind.
  * Long-Trades werden bis `Only Short` nicht deaktiviert.
* **Short-Einstiege** spiegeln die oben genannten Bedingungen wider: Schluss am Tief, Ausbruch unter das vorherige Range-Tief, negative Trendsteigung,
Spread-Filter berücksichtigt und `Only Long` deaktiviert.

### Exit-Management
* Ein fester Stop-Loss (`Stop Loss (pips)`) und ein optionaler Take-Profit (`Take Profit (pips)`) definieren die äußere Schutzhülle.
* Wenn der nicht realisierte Gewinn `Break-even (pips)` erreicht, wird der Stop auf den Einstiegspreis verschoben. Nach `Trailing Activation (pips)` der
Der Stop springt um die Stop-Loss-Distanz nach vorne und sichert den Gewinn genau wie die MetaTrader-Implementierung.
* Zwei Teil-Exits recyceln die ursprünglichen Prozentsätze:
  * `Partial TP1 (pips)` schließt `Partial Ratio 1 (%)` der aktiven Position.
  * `Partial TP2 (pips)` schließt `Partial Ratio 2 (%)` der verbleibenden Position.
Die Logik arbeitet mit dem aktuellen Positionsvolumen, sodass der zweite Teil mit dem skaliert, was nach dem ersten Trimmen übrig bleibt.
* Stops und Ziele reagieren auf Intrabar-Extreme: Ein Long-Trade wird beendet, wenn das Tief der Kerze den Stop durchbricht oder wenn das Hoch erreicht wird
berührt das Gewinnziel; Short-Trades nutzen die symmetrischen Bedingungen.

### Geldmanagement
Fünf Größenmodi ahmen das MetaTrader-Verhalten unter Berücksichtigung von StockSharp-Portfoliodaten nach:
1. **Behoben** – verwendet `Fixed Volume` für jeden Eintrag.
2. **Geometrisch** – skaliert mit der Quadratwurzel des Portfoliowerts (`0.1 * sqrt(balance / 1000) * Geometrical Factor`).
3. **Proportional** – weist einen Bruchteil des freien Eigenkapitals im Verhältnis zum letzten Schlusskurs (`equity * Risk Percent / (price * 1000)`) zu.
4. **Smart** – beginnt mit der proportionalen Zuteilung und reduziert die Größe, wenn mehr als ein aufeinanderfolgender Verlust erkannt wird
mit dem Teiler `Decrease Factor`.
5. **TSSF** – erstellt die Triggered Smart Safe-Factor-Logik neu. Der durchschnittliche Gewinn, der durchschnittliche Verlust und die Gewinnrate werden anhand der meisten berechnet
kürzlich `Last Trades` erzielte Ergebnisse. Die abgeleitete Metrik wechselt zwischen den konfigurierten `TSSF Ratio`-Teilern oder greift zurück
auf ein Minimum von 0,1 Lot, wenn sich die Bedingungen verschlechtern. Alle Lautstärken werden auf die Werte `VolumeStep`, `MinVolume` des Instruments normalisiert.
und `MaxVolume` Einschränkungen.

## Parameter
* **Breakout-Zeitraum** – Anzahl der fertigen Kerzen, die zur Berechnung der Ausbruchshochs und -tiefs verwendet werden.
* **Trendperiode** – Länge des geglätteten gleitenden Durchschnitts, angewendet auf den Medianpreis.
* **Nur kurz / nur lang** – Richtungsumschaltung, die die gegenüberliegende Seite deaktiviert.
* **Max Trades pro Bar** – Trade-pro-Bar-Schutz (0 deaktiviert das Limit).
* **Stop-Loss (Pips)** – anfängliche Stop-Loss-Distanz; Zum Deaktivieren auf 0 setzen.
* **Break-Even (Pips)** – Gewinnschwelle, die den Stop auf das Einstiegsniveau verschiebt.
* **Trailing Activation (Pips)** – Gewinnschwelle, die den Stop um die ursprüngliche Stopgröße nach vorne verschiebt.
* **Partielles TP1 (Pips)** / **Partielles Verhältnis 1 (%)** – Distanz und Prozentsatz für den ersten Teilausstieg.
* **Partielles TP2 (Pips)** / **Partielles Verhältnis 2 (%)** – Distanz und Prozentsatz für den zweiten Teilausstieg.
* **Take Profit (Pips)** – endgültiges Gewinnziel; 0 deaktiviert das harte Ziel.
* **Max Spread (Pips)** – maximal zulässiger Spread zum Zeitpunkt der Eingabe.
* **Geldverwaltung** – wählt den Größenmodus aus (Fest, Geometrisch, Proportional, Smart, TSSF).
* **Festes Volumen** – Basisvolumen, wenn der Geldverwaltungsmodus „Fest“ ist.
* **Geometrischer Faktor** – Multiplikator, der von der geometrischen Größenformel verwendet wird.
* **Risikoprozentsatz** – Prozentsatz des freien Eigenkapitals, das von der proportionalen, intelligenten und TSSF-Dimensionierung verwendet wird.
* **Letzte Trades** – Anzahl der zuletzt realisierten Trades, die für die adaptive Größenanpassung gespeichert werden.
* **Abnahmefaktor** – Teiler, der auf den Smart-Modus angewendet wird, wenn aufeinanderfolgende Verluste auftreten.
* **TSSF-Trigger 1/2/3 und TSSF-Verhältnis 1/2/3** – Schwellenwerte und Teiler für die TSSF-Metrikübergänge.
* **Kerzentyp** – primärer Zeitrahmen, der die Aktualisierung der Indikatoren und die Signalauswertung steuert.

## Zusätzliche Hinweise
* Pip-Werte werden aus der Preisstufe des Wertpapiers abgeleitet; Fünfstellige FX-Symbole ordnen automatisch einen Pip 10 Punkten zu.
* Teilausstiege setzen den Trade-pro-Bar-Zähler nicht zurück und reproduzieren das MetaTrader-Verhalten, bei dem nur neue Einträge gezählt werden.
* Statistiken zum Geldmanagement basieren auf realisierten PnL-Differenzen, so dass die Historie erst nach den ersten Trades aussagekräftig wird
in der Umgebung StockSharp schließen.
* Wenn die besten Gebots-/Briefdaten nicht verfügbar sind, wird der Spread-Filter effektiv deaktiviert und entspricht dem Verhalten des ursprünglichen EA, wenn
Der Broker meldete einen Null-Spread.
