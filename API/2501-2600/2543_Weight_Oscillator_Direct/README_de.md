# Gewichteter Oszillator Direkt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie reproduziert den MetaTrader-Experten **Exp_WeightOscillator_Direct** in der High-Level-API von StockSharp. Sie kombiniert vier klassische Oszillatoren—RSI, Money Flow Index, Williams %R und DeMarker—zu einem einzigen gewichteten Komposit. Das zusammengesetzte Signal wird durch einen konfigurierbaren gleitenden Durchschnitt geglättet und zur Erkennung von Momentum-Schwingungen verwendet. Ein steigendes Komposit öffnet Long-Trades (oder schließt Shorts), wenn die Strategie im „Direct"-Modus arbeitet, während der „Against"-Modus die Logik für konträres Trading umkehrt.

## Indikatorpipeline
1. **Relative Strength Index (RSI)** – normalisierte 0..100-Skala.
2. **Money Flow Index (MFI)** – liquiditätssensitiver Oszillator im Bereich 0..100.
3. **Williams %R (WPR)** – um +100 verschoben, um mit der 0..100-Skala auszurichten.
4. **DeMarker** – mit 100 multipliziert, um mit den anderen Oszillatoren übereinzustimmen.
5. **Glättungsdurchschnitt** – einer der unterstützten gleitenden Durchschnitte (Einfach, Exponentiell, Geglättet, Gewichtet, Jurik, Kaufman).
6. **Zusammengesetzter Oszillator** – gewichteter Durchschnitt der normalisierten Eingaben, geglättet zur Rauschunterdrückung.

Der gewichtete Oszillatorwert wird für jede abgeschlossene Kerze gespeichert. Signale analysieren die letzten drei gespeicherten Werte und können optional eine Anzahl der neuesten Balken über den Parameter *Signal Bar* überspringen, um das ursprüngliche Expertenverhalten nachzuahmen.

## Handelslogik
1. Warten, bis alle Indikatoren und der Glättungsdurchschnitt vollständig gebildet sind.
2. Den geglätteten zusammengesetzten Oszillator für den aktuellen abgeschlossenen Balken berechnen und zum Verlauf hinzufügen.
3. Drei Verlaufswerte abrufen: `current`, `previous`, `prior`, mit durch *Signal Bar* gesteuerten Indizes.
4. Steigungsänderungen erkennen:
   - **Steigend** wenn `previous < prior` **und** `current > previous`.
   - **Fallend** wenn `previous > prior` **und** `current < previous`.
5. Abhängig vom ausgewählten *Trend Mode*:
   - **Direct**: mit der Steigung handeln (`steigend` → Long-Signal, `fallend` → Short-Signal).
   - **Against**: gegen die Steigung handeln (`steigend` → Short, `fallend` → Long).
6. Die Ein-/Ausstiegsschalter anwenden:
   - Entgegengesetztes Exposure schließen, wenn der entsprechende *Close*-Schalter aktiviert ist.
   - Neue Positionen nur öffnen, wenn der jeweilige *Allow*-Schalter aktiviert ist. Die Ordergröße entspricht `Volume + |Position|`, damit die Strategie mit einer einzelnen Marktorder von Short auf Long (oder umgekehrt) wechseln kann.
7. Optionale Stop-Loss- und Take-Profit-Schutzmaßnahmen werden über `StartProtection` mit Abständen in Preisschritten aktiviert.

## Parameter
| Gruppe | Name | Beschreibung |
|-------|------|-------------|
| General | **Candle Type** | Zeitrahmen für Datenabonnement und Indikatorrechnungen. |
| Trading | **Trend Mode** | `Direct` folgt der Oszillatorsteigung, `Against` handelt konträr. |
| Trading | **Signal Bar** | Anzahl der zu überspringenden neuesten geschlossenen Balken (1 = letzter geschlossener Balken). |
| Oscillator | **RSI / MFI / WPR / DeMarker Weight** | Relativer Beitrag jedes Oszillators in der gewichteten Mischung. Null deaktiviert eine Komponente. |
| Oscillator | **RSI / MFI / WPR / DeMarker Period** | Lookback-Länge für jeden Oszillator. |
| Oscillator | **Smoothing Method** | Auf das Komposit angewendeter gleitender Durchschnitt (Einfach, Exponentiell, Geglättet, Gewichtet, Jurik, Kaufman). |
| Oscillator | **Smoothing Length** | Periode für den Glättungsdurchschnitt. |
| Risk Management | **Stop Loss Points** | Abstand in Preisschritten; `0` deaktiviert den Stop. |
| Risk Management | **Take Profit Points** | Abstand in Preisschritten; `0` deaktiviert das Ziel. |
| Trading | **Allow Long/Short Entries** | Öffnen neuer Long/Short-Positionen aktivieren oder deaktivieren. |
| Trading | **Close Shorts/Longs on Signal** | Erlauben, bestehendes Exposure zu schließen, wenn ein entgegengesetztes Signal eintrifft. |

Alle numerischen Parameter sind als `StrategyParam`-Objekte exponiert und ermöglichen Optimierung im StockSharp Designer.

## Verwendungshinweise
- Die Basis-`Volume`-Eigenschaft vor dem Starten der Strategie setzen. Marktorders skalieren automatisch beim Umkehren von Positionen.
- Die Strategie abonniert genau eine Kerzenserie, die von `GetWorkingSecurities()` zurückgegeben wird.
- Schutz-Stops verwenden den `PriceStep` des Instruments, um Punktabstände in absolute Preiswerte umzuwandeln.
- Wenn *Trend Mode* auf `Against` gesetzt ist, ändert sich nur die Signalpolarität; alle anderen Mechaniken bleiben identisch mit dem ursprünglichen Expertenberater.
- Williams %R und DeMarker werden normalisiert, um dieselbe 0..100-Skala wie RSI/MFI zu teilen, entsprechend der ursprünglichen Indikatorlogik.

## Unterschiede zum MQL-Experten
- Der ursprüngliche Indikator unterstützte zusätzliche Glättungstypen (`ParMA`, `JurX`, `VIDYA`, `T3`). In StockSharp bietet die Strategie hochwertige Entsprechungen (Jurik und Kaufman), wobei Jurik für Kompatibilität als Standard verwendet wird.
- Money Flow Index verwendet immer das aggregierte Kerzenvolumen. MetaTrader konnte zwischen Tick- und echten Volumina wechseln; diese Wahl hängt in StockSharp von der Datenquelle ab.
- Risikomanagement wird über `StartProtection` (preisschrittbasiert) statt punktbasierter Anfragen implementiert, liefert aber dasselbe Verhalten, wenn `PriceStep` der Instrument-Kontraktgröße entspricht.

## Einstieg
1. Die Strategie an ein Portfolio und Wertpapier anhängen, das den konfigurierten Kerzentyp unterstützt.
2. Indikatorgewichte/-perioden anpassen und Einstiegsschalter aktivieren oder deaktivieren.
3. Glättungsmethode und -länge wählen, die am besten zur Volatilität des Instruments passen.
4. Stop-Loss/Take-Profit-Abstände in Preisschritten konfigurieren, wenn Schutz erforderlich ist.
5. Strategie ausführen; Signale werden nur auf abgeschlossenen Kerzen ausgeführt, was deterministisches Verhalten gewährleistet.
