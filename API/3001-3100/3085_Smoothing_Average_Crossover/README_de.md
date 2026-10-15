# Strategie Gleitender-Durchschnitt-Crossover mit Glättung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie Gleitender-Durchschnitt-Crossover mit Glättung repliziert die Logik des originalen MQL5-Experten **Smoothing Average (barabashkakvn's edition)**. Sie kombiniert einen konfigurierbaren gleitenden Durchschnitt mit einem in Pips gemessenen Preisabstandsfilter. Wenn sich der Markt weit genug vom geglätteten Durchschnitt entfernt, öffnet die Strategie eine Position in Richtung der Bewegung (oder auf der entgegengesetzten Seite, wenn der Umkehrmodus aktiviert ist). Positionen werden geschlossen, sobald der Preis durch einen erweiterten Kanal um den gleitenden Durchschnitt umkehrt.

## Handelslogik
### Standardmodus (`ReverseSignals = false`)
- **Long einsteigen:** der Schlusskurs steigt über den gleitenden Durchschnitt minus `Entry Delta (pips)`.
- **Short einsteigen:** der Schlusskurs fällt unter den gleitenden Durchschnitt plus `Entry Delta (pips)`.
- **Short schließen:** der Schlusskurs steigt über den gleitenden Durchschnitt plus `Entry Delta (pips) × Close Delta Coefficient`.
- **Long schließen:** der Schlusskurs fällt unter den gleitenden Durchschnitt minus `Entry Delta (pips) × Close Delta Coefficient`.

### Umkehrmodus (`ReverseSignals = true`)
- **Long einsteigen:** der Schlusskurs fällt unter den gleitenden Durchschnitt plus `Entry Delta (pips)`.
- **Short einsteigen:** der Schlusskurs steigt über den gleitenden Durchschnitt minus `Entry Delta (pips)`.
- **Long schließen:** der Schlusskurs fällt unter den gleitenden Durchschnitt minus `Entry Delta (pips) × Close Delta Coefficient`.
- **Short schließen:** der Schlusskurs steigt über den gleitenden Durchschnitt plus `Entry Delta (pips) × Close Delta Coefficient`.

Der gleitende Durchschnitt kann um mehrere Kerzen nach vorne verschoben werden. Die Strategie emuliert dieses Verhalten, indem sie einen kleinen Puffer der neuesten Indikatorwerte hält und den Wert von `MaShift` Balken zurück verwendet. Dies entspricht der verschobenen Linie der originalen MetaTrader-Implementierung.

## Parameter
- `Candle Type` – Datenreihe für Berechnungen.
- `MA Length` – Periode des Glättungsdurchschnitts.
- `MA Shift` – Anzahl der Balken, um die der gleitende Durchschnitt nach vorne verschoben ist.
- `MA Type` – Methode des gleitenden Durchschnitts (einfach, exponentiell, geglättet oder linear gewichtet).
- `Price Source` – in den gleitenden Durchschnitt eingespeister Kerzenkurs (Standard: typischer Kurs).
- `Entry Delta (pips)` – Abstand vom gleitenden Durchschnitt zum Auslösen von Einstiegen. Wird über die Instrument-Pip-Größe in Preis umgerechnet.
- `Close Delta Coefficient` – Multiplikator, der beim Prüfen von Ausstiegsbedingungen auf das Einstiegs-Delta angewendet wird.
- `Reverse Signals` – kehrt die Long/Short-Einstiegslogik um.
- `Trade Volume` – Ordergröße für Long- und Short-Einstiege.

## Risikomanagement
- Orders werden mit dem festen Parameter `Trade Volume` gesendet. Die Strategie skaliert nicht, während eine Position offen ist.
- Alle Ausstiege sind regelbasiert. Es werden keine Hard-Stop-Loss- oder Take-Profit-Orders eingereicht, aber `StartProtection()` wird aufgerufen, um das Sicherheitsnetz auf Plattformebene zu aktivieren.
- Der Umkehrmodus ist für gegenläufiges Verhalten verfügbar, ohne andere Einstellungen zu ändern.

## Implementierungshinweise
- Die Pip-Größe wird aus `Security.PriceStep` abgeleitet. Drei- oder fünfstellige FX-Symbole erhalten die gleiche 10×-Anpassung wie im MQL5-Code.
- Der gleitende Durchschnitt verwendet die `Price Source`-Auswahl, sodass typische, mediane oder andere Kerzenpreise auf die Original-EA-Einstellungen abgestimmt werden können.
- Einstiegs- und Ausstiegsvergleiche verwenden den Kerzenschluss als stabilen Proxy für Bid/Ask-Prüfungen im Quell-Experten.
- Alle Kommentare im C#-Code sind auf Englisch, wie von den Konvertierungsrichtlinien gefordert.
