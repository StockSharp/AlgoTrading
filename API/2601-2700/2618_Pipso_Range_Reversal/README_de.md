# Pipso Bereichs-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des Pipso MQL5 Expert Advisors. Sie fungiert als Mean-Reversion-System, das bei bullischen Ausbrüchen verkauft und bei bärischen Ausbrüchen eines jüngsten Hoch/Tief-Bereichs kauft und dabei die Aktivität auf eine konfigurierbare Handelssitzung beschränkt.

## Kernidee
- Einen Donchian-Kanal aus dem höchsten Hoch und tiefsten Tief der letzten `LookbackPeriod` abgeschlossenen Kerzen erstellen (Standard 36).
- Die obere Grenze überwachen, um Aufwärtsausbrüche zu kontern, und die untere Grenze, um Abwärtsausbrüche zu kontern.
- Positionen nur öffnen, wenn die aktuelle Kerze innerhalb des durch `StartHour` und `EndHour` definierten Handelsfensters beginnt.

## Handelslogik
### Einstiegskriterien
- **Short-Einstieg**: wenn das Kerzenhoch die vorherige Kanalhochmarke berührt oder überschreitet, jede Long-Position schließen und, wenn innerhalb des Sitzungsfensters, `OrderVolume` Kontrakte zum Marktpreis verkaufen. Das Modell erfasst den Einstandspreis als das Kanalhoch.
- **Long-Einstieg**: wenn das Kerzentief die vorherige Kanaltief-Marke berührt oder unterschreitet, jede Short-Position schließen und, wenn Handel erlaubt ist, `OrderVolume` Kontrakte zum Marktpreis kaufen, mit dem Kanaltief als Einstandsreferenz.

### Ausstiegskriterien
- Positionen werden sofort geschlossen, wenn der Preis die gegenüberliegende Kanalseite berührt (spiegelt das Verhalten des Original-EA wider).
- Ein Schutz-Stop wird in einem festen Abstand vom Einstandspreis gesetzt. Der Stop-Abstand entspricht `(channelHigh - channelLow) * (1 + StopRangePercent / 100)`; mit dem Standard `StopRangePercent = 300` liegt der Stop vier Kanalbreiten entfernt.
- Stops werden an Kerzenextremen ausgewertet: Eine Long-Position wird geschlossen, wenn das Kerzentief unter den Stop fällt, und eine Short-Position, wenn das Hoch den Stop überschreitet.

### Sitzungsfilter
- `StartHour` und `EndHour` werden in Börsenzeit angegeben. Wenn `StartHour < EndHour` handelt die Strategie nur zwischen diesen Stunden am gleichen Tag. Wenn `StartHour > EndHour` überspannt das Fenster Mitternacht und ermöglicht Nachtsitzungen (z.B. 21 → 9).
- Wenn das Fenster deaktiviert ist (`StartHour == EndHour`) bleibt die Strategie flach.

## Parameter
- **OrderVolume** *(Standard 0.1)* – Handelsvolumen pro Order.
- **LookbackPeriod** *(Standard 36)* – Anzahl der Kerzen zur Kanalberechnung.
- **StartHour** *(Standard 21)* – Stunde (0–23), wenn die Sitzung öffnet.
- **EndHour** *(Standard 9)* – Stunde (0–23), wenn die Sitzung schließt.
- **StopRangePercent** *(Standard 300)* – zusätzlicher Prozentsatz der Kanalbreite, der zum Rohbereich vor der Konvertierung in eine Stop-Distanz addiert wird.
- **CandleType** *(Standard 1-Stunden-Kerzen)* – Zeitrahmen für Berechnungen.

## Indikatoren und Daten
- Verwendet die `Highest`- und `Lowest`-Indikatoren von StockSharp, um die Kanalgrenzen zu verfolgen.
- Funktioniert mit jedem Wertpapier, das kontinuierliche Kerzendaten entsprechend dem gewählten `CandleType` bereitstellt.
- Der ursprüngliche EA erwartet, dass der Chart-Zeitrahmen den Entscheidungshorizont darstellt; Sie können `CandleType` anpassen, um diese Bedingungen zu reproduzieren.

## Hinweise
- Die Logik arbeitet mit abgeschlossenen Kerzen, um Intrabar-Rauschen zu vermeiden; bei Live-Feeds approximieren die Stop/Einstiegspreise, wo der MQL5-EA mit Ticks interagieren würde.
- Es ist kein Take-Profit-Ziel definiert — Gewinne werden realisiert, wenn der Preis zur gegenüberliegenden Grenze zurückkehrt oder wenn der Stop ausgelöst wird.
- Erwägen Sie, Sitzungsstunden, Bereichslänge und Stop-Multiplikator an die Volatilität des Handelsinstruments anzupassen.
