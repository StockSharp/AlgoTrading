# AFStar-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die AFStar-Strategie sucht nach kurzfristigen Impulsveränderungen, indem sie eine breite
Palette von schnellen/langsamen EMA-Kreuzungen mit einem Williams-%R-Kanalausbruchsfilter
kombiniert. Nur wenn beide Komponenten übereinstimmen, erzeugt die Strategie umsetzbare
Signale.

Ein Kaufpfeil wird erzeugt, wenn mindestens eine schnelle EMA (innerhalb des konfigurierten
Intervalls) eine kompatible langsame EMA von unten kreuzt, während der auf Williams %R
basierende Oszillator das untere Band verlässt, nachdem er sich innerhalb der neutralen Zone
aufgehalten hat. Ein Verkaufspfeil wird durch die symmetrischen Bedingungen für bärische
Kreuzungen und einen Ausbruch aus dem oberen Band erzeugt. Signale werden nach der
konfigurierten Anzahl von Bars, definiert durch den Parameter **Signal Bar**, ausgeführt –
genau wie beim ursprünglichen MetaTrader-Experten.

Sobald eine Position eröffnet ist, kann die Strategie optional schützende Stop-Loss- und
Take-Profit-Levels in Preisschritten anhängen. Diese Schutzmaßnahmen werden bei jeder
geschlossenen Kerze überprüft. Alle Trades verwenden den konstanten Parameter
**Order Volume**, sodass die komplexen Geldverwaltungsregeln der MQL5-Version durch einen
einfacheren Festbetragsansatz ersetzt werden.

## Einstiegslogik

- **Long:**
  - Mindestens eine schnelle EMA innerhalb von `[Start Fast, End Fast]` steigt über eine
    langsame EMA innerhalb von `[Start Slow, End Slow]` mit dem Inkrement `Step Period`.
  - Der Williams-%R-Kanal, bewertet mit Risikowerten im Bereich `[Start Risk, End Risk]`
    und `Risk Step`, erkennt einen Ausbruch über die obere Grenze nach einer Verweildauer
    innerhalb des neutralen Bandes.
  - Optionale Short-Positionen werden vorher geschlossen, wenn **Enable Sell Exits**
    aktiviert ist.
- **Short:**
  - Symmetrischer Kreuzungsausbruch und Williams-%R-Ausbruch in entgegengesetzter Richtung.
  - Optionale Long-Ausstiege erfolgen zuerst, wenn **Enable Buy Exits** aktiviert ist.

## Ausstiegslogik

- Entgegengesetzte Pfeile schließen Positionen, wenn die entsprechenden Ausstiegs-Flags
  aktiviert sind (Kaufpfeile schließen Shorts, Verkaufspfeile schließen Longs).
- Optionale Stop-Loss- und Take-Profit-Levels in Preisschritten können Positionen früher
  schließen, wenn der Preis diese Schwellen erreicht.

## Parameter

- **Order Volume** – Handelsgröße für Marktorders.
- **Candle Type** – Zeitrahmen für Marktdaten (Standard: 4-Stunden-Kerzen).
- **Start Fast / End Fast / Step Period** – schnelle EMA-Spanne für den Kreuzungsscan.
- **Start Slow / End Slow** – langsame EMA-Spanne, die mit den schnellen EMA-Werten gepaart wird.
- **Start Risk / End Risk / Risk Step** – Williams-%R-Risikoscan-Grenzen.
- **Signal Bar** – Anzahl abgeschlossener Bars, die vor der Ausführung eines Signals gewartet wird.
- **Stop Loss (pips)** – optionaler Stop-Loss-Abstand in Preisschritten.
- **Take Profit (pips)** – optionaler Take-Profit-Abstand in Preisschritten.
- **Enable Buy Entries / Enable Sell Entries** – Long- oder Short-Einstiege erlauben.
- **Enable Buy Exits / Enable Sell Exits** – Schließen in entgegengesetzter Richtung aktivieren.

## Hinweise

- Die Strategie hält bis zu 512 aktuelle Kerzen vor, um die AFStar-Logik auszuwerten.
- Falls Preisschritte für das Wertpapier nicht verfügbar sind, wird bei der Berechnung
  von Stop-Loss- und Take-Profit-Abständen der Wert 1 verwendet.
- Signale werden in eine Warteschlange eingereiht, sodass **Signal Bar = 0** sofort
  ausführt, während höhere Werte die Ausführung um diese Anzahl abgeschlossener Bars
  verzögern.
