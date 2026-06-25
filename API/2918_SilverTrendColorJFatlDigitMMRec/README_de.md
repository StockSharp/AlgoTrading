# SilverTrend ColorJFatl Digit MMRec Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie ist eine StockSharp-Portierung des MetaTrader Expert Advisors `Exp_SilverTrend_ColorJFatl_Digit_MMRec`. Sie recreiert die Doppelblock-Architektur, bei der zwei unabhängige Logikmodule ihre eigenen virtuellen Positionsgrößen verwalten und diese zur endgültigen Strategieposition kombinieren:

- **SilverTrend-Block** – liest Kerzenfarben, die vom SilverTrend-Indikator erzeugt werden, um zu erkennen, wann der Preis adaptive Kanalgrenzen überschreitet.
- **ColorJFatl-Block** – berechnet eine gefilterte FATL (Fast Adaptive Trend Line) mit der veröffentlichten Gewichtstabelle und einem EMA-basierten Glätter, der den in MetaTrader verwendeten Jurik-Gleitenden Durchschnitt emuliert.

Beide Module können unabhängig Long- und Short-Trades öffnen, entgegengesetztes Exposure bei neuen Signalen schließen und ihre eigenen Stop-Loss- und Take-Profit-Abstände anwenden. Die endgültige Strategieposition entspricht der Summe der von beiden Blöcken verwalteten virtuellen Positionen.

## Standardkonfiguration

- Symbol: das in StockSharp ausgewählte Strategie-Wertpapier.
- Zeitrahmen: Beide Module verwenden standardmäßig 6-Stunden-Kerzen (konfigurierbar über Parameter).
- Ordergröße: Jedes Modul sendet Marktorders mit einem separaten Volumenparameter (Standard `1`).

## Indikatoren und Signallogik

### SilverTrend-Block

1. Erstellt einen rollierenden Preiskanal aus den letzten `SSP` Kerzen.
2. Wendet den ursprünglichen `Risk`-Versatz `(33 - Risk) / 100` an, um Kanalgrenzen innerhalb des Hoch/Tief-Bereichs zu verschieben.
3. Färbt jede Kerze gemäß dem aktiven Trend ein (`0`/`1` bullisch, `3`/`4` bearisch, `2` neutral), wie der MetaTrader-Indikator.
4. Signale:
   - **Long** wenn die Kerze an der konfigurierten `Signal Bar` bullisch wird, während die vorherige Kerze es nicht war (`color < 2` und vorherige `> 1`).
   - **Short** wenn sie bearisch wird, während die vorherige Kerze es nicht war (`color > 2` und vorherige `< 3`).
5. Optionale Stop-Loss- und Take-Profit-Niveaus werden in Punkten unter Verwendung des Wertpapier-Preisschritts gemessen.

### ColorJFatl-Block

1. Erstellt einen FATL-Wert durch Anwendung der offiziellen Koeffizienttabelle auf die gewählte `Applied Price`-Quelle.
2. Glättet das Ergebnis mit einer EMA der Länge `JMA Length` (der Jurik-Phasenwert wird aus Kompatibilitäts- und Dokumentationsgründen beibehalten).
3. Färbt die FATL-Linie gemäß Steigung: `2` für steigend, `0` für fallend und `1` für flache Segmente.
4. Signale:
   - **Long** wenn die FATL-Farbe zu `2` wechselt, während die vorherige Farbe `0` oder `1` war.
   - **Short** wenn die Farbe zu `0` wechselt, während der vorherige Wert `1` oder `2` war.
5. Jede Richtung kann optional die entgegengesetzte Block-Position schließen, bevor ein neuer Trade geöffnet wird.

## Risikomanagement

- SilverTrend und ColorJFatl verwalten jeweils ihren eigenen Einstiegspreis und Stop-/Ziel-Abstände.
- Wenn ein Stop oder Ziel getroffen wird, schließt nur der betroffene Block seine virtuelle Position (der andere Block kann offen bleiben).
- Wenn beide Blöcke in dieselbe Richtung übereinstimmen, akkumulieren sich ihre Volumina.

## Parameter

| Gruppe | Name | Beschreibung |
| --- | --- | --- |
| SilverTrend | `Silver Candle Type` | Kerzen-Abonnement für den SilverTrend-Indikator. |
| SilverTrend | `SSP` | Länge des rollierenden Hoch/Tief-Bereichs. |
| SilverTrend | `Risk` | Kanal-Kontraktionsfaktor (ursprünglicher `Risk`-Wert). |
| SilverTrend | `Signal Bar` | Balkenversatz für das Signal (0 = aktuell geschlossener Balken, 1 = vorheriger Balken, etc.). |
| SilverTrend | `Allow Silver Long/Short` | Einträge für jede Richtung aktivieren. |
| SilverTrend | `Close Silver Long/Short` | Automatisches Schließen der entgegengesetzten Position erlauben. |
| SilverTrend | `Silver Volume` | Volumen für Trades, die vom SilverTrend-Block geöffnet werden. |
| SilverTrend | `Silver SL/TP` | Stop-Loss- und Take-Profit-Abstände in Punkten. |
| ColorJFatl | `Color Candle Type` | Kerzen-Abonnement für die FATL-Berechnungen. |
| ColorJFatl | `JMA Length` | Länge des EMA-Glätters, der JMA emuliert. |
| ColorJFatl | `JMA Phase` | Aus Vollständigkeitsgründen beibehalten (kein direkter Einfluss innerhalb von StockSharp). |
| ColorJFatl | `Applied Price` | Quellenpreis (Schluss, Median, Typisch, Trend-Follow usw.). |
| ColorJFatl | `Digits` | Dezimalpräzision auf den FATL-Wert angewendet. |
| ColorJFatl | `Color Signal Bar` | Balkenversatz für FATL-Signale. |
| ColorJFatl | `Allow/Close`-Schalter | Einträge und Auto-Exits für jede Richtung aktivieren. |
| ColorJFatl | `Color Volume` | Volumen für Trades, die vom ColorJFatl-Block geöffnet werden. |
| ColorJFatl | `Color SL/TP` | Stop-Loss- und Take-Profit-Abstände in Punkten für den Block. |

## Hinweise

- Die Strategie abonniert beide Kerzen-Streams, auch wenn sie identisch sind. Doppelte Abonnements werden intern von StockSharp gehandhabt.
- Der Jurik-Phasenparameter wird beibehalten, um nah am ursprünglichen Expert Advisor zu bleiben. StockSharp's EMA-basierter Glätter repliziert das gekrümmte FATL-Verhalten, während der Parameter für zukünftige Erweiterungen verfügbar bleibt.
- Sicherstellen, dass das Wertpapier `PriceStep` gesetzt hat, um punktbasierte Risikolimits zu verwenden.

## Verwendungshinweise

1. Die `Volume`-Eigenschaft der Strategie setzen oder blockspezifische Volumenparameter anpassen, um das absolute Exposure zu kontrollieren.
2. Die Aktivierungs-/Deaktivierungsflags verwenden, um jeden Block separat zu testen, bevor sie kombiniert werden.
3. Da die Blöcke unabhängig operieren, kann die Strategie gleichzeitig ein Netto-Long und Short halten (zum Beispiel Long von SilverTrend und Short von ColorJFatl) – die resultierende Position ist die algebraische Summe beider.
4. `SSP`, `Risk` und `JMA Length` für den Zielmarkt optimieren, wenn automatische Parametersuche geplant ist.
