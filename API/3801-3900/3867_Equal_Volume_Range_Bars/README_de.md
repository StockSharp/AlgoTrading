# Gleiche Lautstärke- und Bereichsbalken
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Equal Volume & Range Bars portiert das MetaTrader 4-Skript `equalvolumebars.mq4` nach StockSharp. Das ursprüngliche Skript generierte Offline-Charts, deren Kerzen entweder nach einer festen Anzahl von Ticks oder nachdem der Preis einen konfigurierbaren Punktbereich durchlaufen hatte, geschlossen wurden. Die Strategie reproduziert die gleiche Kerzenbildungslogik innerhalb der StockSharp-Umgebung: Sie lauscht auf Live-Ticks, lädt optional historische M1-Kerzen vor und gibt detaillierte Protokolleinträge aus, wann immer ein synthetischer Balken abgeschlossen ist.

## Logik der Kerzenkonstruktion
* **Zwei Betriebsmodi** – `EqualVolumeBars` schließt den Balken, sobald das kumulierte Tick-Volumen den konfigurierten Schwellenwert überschreitet, während `RangeBars` erfordert, dass der Hoch-Tief-Bereich der Kerze (gemessen in Wertpapierpreisschritten) denselben numerischen Schwellenwert überschreitet.
* **Tick-gesteuerte Aktualisierungen** – bei jeder Handelsaktualisierung werden die aktuellen Höchst-, Tiefst- und Schlusskurse sowie das Tick-Volumen der Kerze aktualisiert. Wenn der Schwellenwert überschritten würde, finalisiert die Strategie die vorherige Kerze mit den vorhandenen Statistiken und startet sofort einen neuen Balken mit dem aktuellen Tick als erstem Eintrag.
* **Minutenverlaufs-Seeding (optional)** – wenn `FromMinuteHistory` aktiviert ist, spielt die Strategie abgeschlossene M1-Kerzen als Folge synthetischer Ticks ab (Eröffnung → Zwischenextreme → Schluss). Dies entspricht in etwa dem Initialisierungsschritt des Offline-Diagramms, ohne dass externe CSV-Tick-Dateien erforderlich sind.
* **Monotone Zeitstempel** – Der Builder erzwingt strikt ansteigende Zeitstempel, damit Protokollkonsumenten oder nachgeschaltete Module die Daten laden können, ohne auf doppelte Zeitschlüssel zu stoßen.

## Parameter
* **Arbeitsmodus** – wählt zwischen der Kerzenkonstruktion `EqualVolumeBars` und `RangeBars`.
* **Ticks In Bar** – Anzahl der Ticks pro Kerze (Modus mit gleichem Volumen) oder Punktspanne, gemessen in Preisschritten (Range-Modus).
* **Minutenverlauf verwenden** – ermöglicht die synthetische Wiedergabe fertiger M1-Kerzen, bevor Live-Ticks eintreffen.
* **Minutenkerzentyp** – Kerzenabonnement, das für den historischen Seeding-Schritt verwendet wird (standardmäßig ein Zeitrahmen von einer Minute).

## Zusätzliche Hinweise
* Die Strategie leitet die Punktgröße von `Security.PriceStep` ab (wobei auf `Security.MinPriceStep` oder `0.0001` zurückgegriffen wird, wenn keine Metadaten verfügbar sind), um die von MetaTrader verwendete Konstante `_Point` widerzuspiegeln.
* Anstatt `.hst`-Dateien zu schreiben und ein Diagrammfenster zu aktualisieren, protokolliert der C#-Port jede fertige Kerze mit vollständigen OHLCV-Daten, sodass es einfach ist, eine andere Komponente zu füttern oder Ergebnisse mit dem MT4-Offline-Diagrammersteller zu vergleichen.
* Es werden nie Bestellungen aufgegeben; Der Kurs konzentriert sich ausschließlich auf die Datentransformation, genau wie das Originalskript.
* Es wird nur die C#-Version bereitgestellt. Gemäß den Konvertierungsanforderungen wird absichtlich auf eine Python-Version und einen Python-Ordner verzichtet.
