# Bw WiseMan-1 Ausbruchsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des MetaTrader-Expert-Advisors **Exp_BW-wiseMan-1**. Sie automatisiert Bill Williams' WiseMan-1-Ausbruchslogik, die auf dem Alligator-Indikator aufgebaut ist. Signale werden erzeugt, wenn eine abgeschlossene Kerze aus den Kiefern des Alligators ausbricht und gleichzeitig die jüngsten Swing-Extreme bricht. Ein optionaler Gegentrendmodus tauscht die Signale aus, sodass die Strategie dieselben Ausbrüche verblassen lassen kann.

## Kernidee
- Berechnen Sie den Bill Williams Alligator mit geglätteten gleitenden Durchschnitten des Medianpreises (hoch + tief) / 2.
- Verschieben Sie die Kiefer-, Zahn- und Lippenlinien nach vorne durch konfigurierbare Versätze, um die Visualisierung des ursprünglichen Indikators zu entsprechen.
- Bestätigen Sie einen Ausbruch nur, wenn die aktuelle Kerze über die Hochs oder Tiefs der letzten *N* Balken hinausgeht und sicherstellen, dass die Bewegung stärker als das jüngste Rauschen ist.
- Verzögern Sie die Ausführung um die ausgewählte Anzahl abgeschlossener Kerzen, damit der Trader bei Bedarf auf ältere Signale reagieren kann.

## Handelsregeln
### Long-Richtung
1. Der Balken muss **unterhalb** aller drei Alligator-Linien enden (Hochpreis kleiner als Kiefer, Zähne und Lippen).
2. Der Schlusskurs muss sich in der oberen Hälfte der Kerze befinden, d.h. über dem Median der Kerze.
3. Das Tief der Kerze muss strikt niedriger sein als die Tiefs der vorherigen `Back`-Balken.
4. Wenn das Signal nach der `SignalBar`-Verzögerung aktiv wird:
   - Schließen Sie jeden offenen Short, wenn `Close Short` aktiviert ist.
   - Eröffnen Sie eine neue Long-Position, wenn `Enable Long` aktiviert ist und derzeit keine Position offen ist.

### Short-Richtung
1. Der Balken muss **oberhalb** aller drei Alligator-Linien enden (Tiefpreis größer als Kiefer, Zähne und Lippen).
2. Der Schlusskurs muss sich in der unteren Hälfte der Kerze befinden, d.h. unter dem Median der Kerze.
3. Das Hoch der Kerze muss größer sein als die Hochs der vorherigen `Back`-Balken.
4. Wenn das Signal aktiv wird:
   - Schließen Sie jeden bestehenden Long, wenn `Close Long` aktiviert ist.
   - Eröffnen Sie eine neue Short-Position, wenn `Enable Short` aktiviert ist und keine aktuelle Position vorhanden ist.

### Gegentrendmodus
Das Setzen von `Counter-Trend Mode` auf **true** tauscht die Kauf- und Verkaufssignale aus, sodass die Strategie Trades gegen die Alligator-Ausbruchsrichtung eingeht.

## Parameter
- **Candle Type** – Zeitrahmen zur Erstellung von Kerzen und Berechnung aller Indikatorwerte (Standard: 1 Stunde).
- **Counter-Trend Mode** – Ausbruchslogik umkehren, um gegen den primären Trend zu handeln (Standard: aktiviert, entsprechend dem ursprünglichen EA).
- **Breakout Depth (`Back`)** – Anzahl der vorherigen Balken, die mit dem aktuellen Hoch/Tief bei der Ausbruchsvalidierung verglichen werden (Standard: 2).
- **Jaw Length / Shift** – geglättete MA-Länge und Vorwärtsverschiebung für die Kieferlinie (Standards: 13 / 8).
- **Teeth Length / Shift** – geglättete MA-Länge und Vorwärtsverschiebung für die Zahnlinie (Standards: 8 / 5).
- **Lips Length / Shift** – geglättete MA-Länge und Vorwärtsverschiebung für die Lippenlinie (Standards: 5 / 3).
- **Signal Bar** – Anzahl bereits abgeschlossener Kerzen, die vor der Ausführung eines erkannten Signals gewartet werden sollen (Standard: 1).
- **Enable Long / Enable Short** – Schalter zum Öffnen neuer Long- oder Short-Positionen.
- **Close Long / Close Short** – Schalter zum Schließen entgegengesetzter Positionen, wenn das Signal ausgelöst wird.

## Hinweise
- Die Strategie verlässt sich ausschließlich auf Market Orders und setzt keine harten Stop-Loss- oder Take-Profit-Niveaus. Jeder Ausstieg wird durch das entgegengesetzte Signal oder durch Deaktivierung des entsprechenden Schließ-Schalters gesteuert.
- Alle Berechnungen werden auf fertigen Kerzen durchgeführt; partielle Intrabar-Daten werden ignoriert, um mit dem ursprünglichen MetaTrader-Experten konsistent zu bleiben.
- Das Volumen wird von den StockSharp-Strategieeinstellungen geerbt. Passen Sie das Basisvolumen in der Plattformkonfiguration an, wenn Sie eine andere Positionsgröße benötigen.
