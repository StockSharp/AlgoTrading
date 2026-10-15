# Experten-Alligator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Expert Alligator-Strategie** ist eine originalgetreue StockSharp-Portierung des integrierten Expertenberaters `Expert_Alligator.mq5` von MetaTrader 5. Der ursprüngliche Experte stützt seine Handelsentscheidungen auf den Indikator Bill Williams' Alligator, der aus drei geglätteten, in die Zukunft verschobenen gleitenden Durchschnitten besteht: dem Kiefer (blau), den Zähnen (rot) und den Lippen (grün). Durch die Überwachung, wie sich diese Linien zusammenziehen und ausdehnen, identifiziert EA neue Kreuzungen und wartet darauf, dass sich der „Mund“ öffnet, bevor ein weiterer Handel eingegangen werden kann. Diese C#-Konvertierung erstellt denselben Workflow mit der übergeordneten Strategie API und der Indikatorsuite von StockSharp neu.

## Handelslogik

1. **Indikatorvorbereitung**
   - Erstellen Sie drei geglättete gleitende Durchschnitte des Medianpreises unter Verwendung der klassischen Alligator-Längen (13, 8 und 5) und wenden Sie die standardmäßigen Vorwärtsverschiebungen (8, 5 bzw. 3 Balken) an.
   - Speichern Sie einen fortlaufenden Verlauf jeder verschobenen Linie, damit vergangene und zukünftige Offsets, die von MetaTrader EA (z. B. `LipsTeethDiff(-2)`) verwendet werden, sicher ausgewertet werden können.

2. **Eintrittsbedingungen**
   - *Long Trades*: Wird ausgelöst, wenn die Lippen-Zähne- und Zähne-Kiefer-Spreads drei aufeinanderfolgende verschobene Balken lang geschrumpft sind, während sie über Null blieben. Dies reproduziert die Anforderung von EA, dass die grüne Linie die rote Linie nach unten kreuzt, was eine nach oben gerichtete Mundöffnung bestätigt.
   - *Short-Trades*: Spiegeln Sie die Long-Logik wider, wobei sich die Spreads unter Null verengen, was darauf hindeutet, dass die Lippen nach oben durch die Zähne und den Kiefer kreuzen.
   - Nachdem ein Trade eröffnet wurde, setzt die Strategie ein internes `crossed`-Flag, das weitere Einträge blockiert, bis sich die drei Alligator-Spreads mindestens um den konfigurierten **Cross Measure**-Abstand erweitern.

3. **Ausstiegsbedingungen**
   - *Long-Positionen* schließen, wenn der Lippen-Zähne-Spread beim zuletzt verschobenen Wert negativ wird, während er bei den beiden älteren Werten positiv bleibt (Indizes `-1`, `0`, `1` im ursprünglichen EA).
   - *Short-Positionen* werden beendet, wenn die gleiche Sequenz in die entgegengesetzte Richtung eintritt.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `Order Volume` | Handelsgröße in Lots oder Kontrakten, die an `BuyMarket`/`SellMarket` übergeben wurden. | `0.1` |
| `Candle Type` | Zeitrahmen für das Kerzenabonnement. | `1 Hour` |
| `Jaw Period` | Geglättete gleitende Durchschnittslänge für die Kieferlinie. | `13` |
| `Jaw Shift` | Vorwärtsverschiebung (in Balken) der Kieferlinie. | `8` |
| `Teeth Period` | Geglättete gleitende Durchschnittslänge für die Zahnlinie. | `8` |
| `Teeth Shift` | Vorwärtsverschiebung (in Balken) der Zahnlinie. | `5` |
| `Lips Period` | Geglättete gleitende Durchschnittslänge für die Lippenlinie. | `5` |
| `Lips Shift` | Vorwärtsverschiebung (in Balken) der Lippenlinie. | `3` |
| `Cross Measure` | Minimaler Spread (in MetaTrader Punkten), der sich nach einem Crossover entwickeln muss, bevor ein weiterer Trade ausgelöst werden kann. | `5` |

## Hinweise zur Implementierung

- Die Strategie berechnet den Medianpreis `(High + Low) / 2` für jede fertige Kerze und speist ihn in drei `SmoothedMovingAverage`-Instanzen ein.
- Verschobene Historien werden mit Arrays fester Größe implementiert, um die Art und Weise widerzuspiegeln, wie MetaTrader zukünftige Indizes wie `-1` oder `-2` verfügbar macht, sobald die Alligator-Zeilen nach vorne verschoben werden.
- Der Wert MetaTrader `_Point` wird durch den Wert `PriceStep` des Symbols emuliert. Wenn letzterer nicht verfügbar ist, fällt der Code auf `10^-Decimals` oder `0.0001` zurück.
- Die Diagrammausgabe stimmt mit EA überein, indem Kiefer, Zähne und Lippen auf dem primären Kerzenfeld dargestellt werden, was eine schnelle visuelle Validierung ermöglicht.

## Nutzung

1. Hängen Sie die Strategie an einen `Connector` mit einem Wertpapier an, das den gewünschten Kerzentyp bereitstellt (Standard-Einstundenkerzen).
2. Rufen Sie `Start()` auf, sobald der Marktdatenstrom bereit ist.
3. Optional: Passen Sie die Längen, Verschiebungen oder den Cross-Measure-Schwellenwert von Alligator an, um benutzerdefiniertes Verhalten zu testen.
4. Überwachen Sie Positionen und Leistung über die Standardschnittstellen von StockSharp.

Es sind keine zusätzlichen Trailing Stops oder Money-Management-Module erforderlich, da das ursprüngliche EA eine feste Lotgröße verwendet und sich für die Handelsverwaltung ausschließlich auf die Liniengeometrie von Alligator verlässt.
