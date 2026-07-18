# Chaos Trader Lite Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Chaos Trader Lite Strategie repliziert Bill Williams' drei weisen Männer Einstiegstechniken unter Verwendung der High-Level-API von StockSharp. Sie analysiert jede fertige Kerze des konfigurierten Zeitrahmens (standardmäßig 1 Stunde) und platziert Stop-Orders, wenn eine der folgenden Bedingungen erfüllt ist:

1. **Erster Weiser Mann – Divergenzbalken**: erkennt bullische oder bärische Divergenzkerzen und erfordert einen Mindestabstand zwischen dem Preis und der Alligator-Lippen-Linie.
2. **Zweiter Weiser Mann – Awesome Oscillator-Beschleunigung**: wartet auf fünf aufeinanderfolgende Awesome Oscillator-Werte, die beschleunigendes Momentum zeigen.
3. **Dritter Weiser Mann – Fraktal-Ausbruch**: bestätigt ein Fraktal zwei Kerzen zurück und prüft, ob der Preis weit genug von der Alligator-Zähne-Linie entfernt handelt, bevor eine Ausbruchsorder eingereiht wird.

Wann immer ein Long-Setup erscheint, storniert die Strategie bestehende Sell-Stops, schließt Short-Positionen, platziert einen neuen Buy-Stop knapp über dem vorherigen Hoch und registriert einen Schutz-Stop unter der Kerze. Das Gegenteil geschieht für Short-Setups. Schutz-Stops werden bei jedem Balken überwacht; wenn der Preis das gespeicherte Niveau kreuzt, wird die Position zum Marktpreis geschlossen.

## Indikatoren und Berechnungen

- **Alligator-Lippen**: 5-Perioden geglätteter gleitender Durchschnitt des Median-Preises, um drei Kerzen nach vorne verschoben. Die Strategie hält eine Warteschlange, damit der mit der aktuellen Kerze ausgerichtete Wert der MetaTrader-Implementierung entspricht.
- **Alligator-Zähne**: 8-Perioden geglätteter gleitender Durchschnitt des Median-Preises, um fünf Kerzen nach vorne verschoben. Der verschobene Wert treibt den Dritter-Weiser-Mann-Filter an.
- **Awesome Oscillator**: StockSharp's eingebauter Indikator (5 vs 34 SMA des Median-Preises) liefert die Momentum-Reihe für den zweiten weisen Mann.
- **Fraktale**: Der Code inspiziert das Hoch/Tief der Kerze, die zwei Balken hinter dem letzten Balken liegt. Ein gültiges Fraktal erfordert, dass diese Kerze höher (oder niedriger) ist als die zwei Kerzen auf beiden Seiten.

## Handelslogik

1. Abonnieren Sie den angeforderten Kerzentyp und verarbeiten Sie nur fertige Kerzen.
2. Aktualisieren Sie Alligator- und Awesome Oscillator-Indikatoren und speichern Sie verschobene Werte.
3. Bewerten Sie die Bedingungen der weisen Männer:
   - Divergenzbalken muss in der oberen (für Bullen) oder unteren (für Bären) Hälfte der Kerze schließen und einen Abstand von den Lippen größer als `MagnitudePips * PriceStep` zeigen.
   - AO-Beschleunigung erfordert fünf Werte: `AO[1] > AO[2] > AO[3] > AO[4]` und `AO[4] < AO[5]` für Longs, gespiegelt für Shorts.
   - Fraktal-Ausbruch prüft, ob der Preis über (oder unter) dem bestätigten Fraktal und über (oder unter) den Alligator-Zähnen plus dem Magnitudenschwellenwert schließt.
4. Wenn ein Setup aktiv ist, platzieren Sie eine `BuyStop`- oder `SellStop`-Order mit Volumen `Volume` beim Kerzenhoch plus einem Kursschritt (oder Tief minus einem Schritt). Stornieren Sie den entgegengesetzten Stop und glätten Sie Gegenpositionen.
5. Aktualisieren Sie gespeicherte Stop-Loss-Niveaus: Long-Stops folgen nach oben, Short-Stops nach unten. Wenn eine Kerze den gespeicherten Stop durchbricht, schließt die Strategie die offene Position zum Marktpreis.

## Parameter

- `MagnitudePips` *(Standard 10)* – minimaler Pip-Abstand zwischen dem Divergenzbalken und den Alligator-Lippen.
- `UseFirstWiseMan` *(Standard true)* – Einstieg per Divergenzbalken aktivieren oder deaktivieren.
- `UseSecondWiseMan` *(Standard true)* – Einstieg per Awesome Oscillator-Beschleunigung aktivieren oder deaktivieren.
- `UseThirdWiseMan` *(Standard true)* – Einstieg per Fraktal-Ausbruch aktivieren oder deaktivieren.
- `Volume` *(Standard 0.01)* – Ordergröße für Stop-Einstiege.
- `CandleType` *(Standard 1 Stunde)* – von der Strategie verarbeiteter Datentyp.

## Hinweise

- Bid/Ask-Prüfungen aus dem ursprünglichen MQL4-Code werden mit dem Kerzen-Schlusskurs in StockSharp approximiert.
- Margin- und Volumen-Validierungsroutinen aus MetaTrader werden ausgelassen, weil StockSharp die Order-Validierung intern behandelt.
- Stop-Orders werden storniert, wenn das entgegengesetzte Setup erscheint, um conflicting Pending Orders zu vermeiden, entsprechend dem `CloseAll`-Verhalten des EA.
