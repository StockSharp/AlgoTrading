# Strategie Exp Color PEMA Digit TM Plus MMRec Duplex (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Strategie re­kreiert den Expert Advisor „Exp_ColorPEMA_Digit_Tm_Plus_MMRec_Duplex" mithilfe der High-Level-API von StockSharp. Sie arbeitet mit zwei unabhängigen Pentuple-Exponential-Moving-Average-(PEMA-)Streams, die unterschiedliche Zeitrahmen und Preisquellen verwenden können. Das Long-Modul eröffnet Trades, wenn die PEMA-Steigung auf bullisch wechselt, während das Short-Modul auf bärische Wendungen reagiert. Jede Seite unterstützt indikatorgesteuerte Ausstiege und einen Sicherheitstimer, der die Position nach einer konfigurierbaren Anzahl von Minuten zwangsweise schließt.

## Indikatoren
* **Pentuple EMA** – benutzerdefinierter Indikator, der acht exponentielle Durchschnitte gleicher Länge kaskadiert und diese mit den klassischen Koeffizienten (8, -28, 56, -70, 56, -28, 8, -1) kombiniert. Der Indikator liefert sowohl den aktuellen Wert als auch die vorherige Stichprobe, sodass die Strategie die Steigungsrichtung (aufwärts, abwärts, flach) klassifizieren kann.
* **Farblogik** – die Steigung wird drei diskreten Zuständen zugeordnet: aufwärts (grün), abwärts (magenta) und neutral (grau), womit das Verhalten des originalen ColorPEMA-Indikators reproduziert wird.

## Signalerzeugung
### Long-Modul
1. Wartet auf eine abgeschlossene Kerze im ausgewählten Long-Zeitrahmen.
2. Fordert den PEMA-Wert mit dem konfigurierten Preismodus und den Rundungsstellen an.
3. Bewertet den Farbzustand `SignalBar` Kerzen zurück und vergleicht ihn mit dem vorherigen Balken.
4. **Einstieg**: Wenn die Farbe auf `Up` wechselt und Einträge erlaubt sind, kauft die Strategie mit dem gemeinsamen `TradeVolume` und speichert die Einstiegszeit.
5. **Ausstieg**: Wenn die Farbe auf `Down` wechselt, schließt die Strategie die Long-Position, sofern indikatorbasierte Ausstiege aktiviert sind.
6. **Zeitwächter**: Wenn die offene Long-Position länger als `LongTimeExitMinutes` besteht, wird sie unabhängig vom Indikatorstatus geschlossen.

### Short-Modul
Die Short-Seite wiederholt denselben Arbeitsablauf unabhängig:
1. Überwachung der Short-Zeitrahmen-Kerzen.
2. Berechnung der Short-PEMA-Reihe.
3. `ShortSignalBar` Kerzen zurückschauen, um einen Wechsel zur `Down`-Farbe zu erkennen.
4. **Einstieg**: Wenn die Farbe bärisch wird und Shorts aktiviert sind, verkauft die Strategie.
5. **Ausstieg**: Wenn die Farbe `Up` wird, wird der Short gedeckt, sofern Ausstiege erlaubt sind.
6. **Zeitwächter**: Wenn `ShortTimeExitMinutes` überschritten wird, wird die Short-Position geschlossen.

## Risikomanagement
* Verwendet den Parameter `TradeVolume` zur Konfiguration der Standard-Ordergröße.
* Optionale Stop-Loss- und Take-Profit-Abstände können in Preisschritten festgelegt werden. Wenn einer davon positiv ist, aktiviert die Strategie `StartProtection` mit marktbasierten Exit-Orders und spiegelt damit den Geldverwaltungsschutz der MQL-Version.
* Unabhängige zeitbasierte Exit-Timer für Long- und Short-Module verhindern, dass Trades unbegrenzt laufen.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `LongCandleType` | Zeitrahmen für den Long-Indikatorstream. |
| `ShortCandleType` | Zeitrahmen für den Short-Indikatorstream. |
| `LongEmaLength`, `ShortEmaLength` | Glättungslängen des Pentuple EMA (Bruchwerte werden unterstützt). |
| `LongPriceMode`, `ShortPriceMode` | Angewendeter Preismodus für jeden Stream (Schluss, Eröffnung, Hoch, Tief, Median, Typisch, Gewichtet, Einfach, Quartal, Trendfolge und Demark). |
| `LongDigits`, `ShortDigits` | Dezimalrundung der berechneten PEMA-Werte. |
| `LongSignalBar`, `ShortSignalBar` | Anzahl abgeschlossener Balken zurück zur Beurteilung des Farbwechsels. |
| `LongAllowOpen`, `ShortAllowOpen` | Neue Einträge für jede Seite aktivieren/deaktivieren. |
| `LongAllowClose`, `ShortAllowClose` | Indikatorbasierte Ausstiege aktivieren/deaktivieren. |
| `LongAllowTimeExit`, `ShortAllowTimeExit` | Den zeitbasierten Exit-Wächter ein- oder ausschalten. |
| `LongTimeExitMinutes`, `ShortTimeExitMinutes` | Maximale Haltedauer in Minuten für Long- und Short-Trades. |
| `TradeVolume` | Standardvolumen für Market-Orders. |
| `StopLossSteps`, `TakeProfitSteps` | Optionale Schutzabstände in Preisschritten des Instruments. |

## Hinweise
* Die Strategie abonniert beide Long- und Short-Kerzenreihen; wenn beide Parameter auf denselben Zeitrahmen zeigen, verwendet StockSharp den Datenfeed automatisch wieder.
* Beide Module teilen dieselben Instrument- und Volumeneinstellungen und garantieren damit ein symmetrisches Verhalten.
* Indikatorberechnungen werden nur bei abgeschlossenen Kerzen ausgeführt, um Neuzeichnung zu vermeiden.
