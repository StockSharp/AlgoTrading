# FineTuning MA Candle Duplex-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- C#-Port des MetaTrader 5-Expertenberaters **Exp_FineTuningMACandle_Duplex**.
- Repliziert den FineTuningMA-Kerzenindikator in zwei unabhängigen Strömen, damit Long- und Short-Logik separat konfiguriert werden kann.
- Entwickelt für die High-Level-Strategie-API von StockSharp: Subscriptions, Indikatoren, Risikomanagement und Chart-Zeichnung werden alle automatisch vom Framework verwaltet.

## FineTuningMA-Kerzenmodell
- Der ursprüngliche Indikator erstellt eine synthetische Kerze durch Anwendung von drei gewichteten Exponenten (`Rank1`–`Rank3`) und entsprechenden Verschiebungskoeffizienten auf die letzten `Length` Bars.
- Die resultierenden gewichteten Eröffnungs- und Schlusswerte werden verglichen, um einen Farbcode zu generieren: `2` für bullisch, `1` für neutral, `0` für bearisch.
- Wenn der reale Kerzenkörper kleiner als der konfigurierbare `Gap` ist, wird der synthetische Eröffnungskurs auf den vorherigen synthetischen Schlusskurs gesetzt. Dies reproduziert die "Flat-Body"-Logik der MQL5-Version.
- Der Indikator in diesem Port emittiert nur den Farbstrom (Dezimalwerte 0/1/2), da die Handelsregeln ausschließlich von Farbübergängen abhängen.

## Handelslogik
1. Abonniert zwei Kerzendatenfeeds (`LongCandleType` und `ShortCandleType`). Diese können auf denselben oder verschiedene Zeitrahmen zeigen.
2. Für jeden Feed wird eine dedizierte FineTuningMA-Indikatorinstanz mit eigenen Gewichtungsparametern und Signal-Offset (`SignalBar`) erstellt.
3. Abgeschlossene Kerzenereignisse werden mit folgenden Regeln verarbeitet:
   - **Long-Exit** – wenn die vorherige Farbe gleich `0` ist, wird die bestehende Long-Position geschlossen.
   - **Long-Einstieg** – wenn die vorherige Farbe gleich `2` ist und die aktuelle Farbe sich von `2` weg veränderte, wird eine Kauforder gesendet (nach Schließen eines eventuellen Shorts).
   - **Short-Exit** – wenn die vorherige Farbe gleich `2` ist, wird die bestehende Short-Position geschlossen.
   - **Short-Einstieg** – wenn die vorherige Farbe gleich `0` ist und die aktuelle Farbe sich von `0` weg veränderte, wird eine Verkaufsorder gesendet (nach Schließen eines eventuellen Longs).
4. Das Ordervolumen wird durch `OrderVolume` gesteuert. Wenn eine Umkehr erforderlich ist, addiert die Strategie automatisch die absolute aktuelle Position, damit die Position in einer einzigen Marktorder gedreht wird.
5. Optionale Schutzbarrieren (`TakeProfitPoints`, `StopLossPoints`) werden in Preispunkte übersetzt und über `StartProtection` angewendet.

## Parameter
### Long-Strom
- `LongCandleType` – Kerzendatentyp (Zeitrahmen) für den Long-Indikatorstrom.
- `LongLength` – Anzahl der Bars in der gewichteten Berechnung.
- `LongRank1`, `LongRank2`, `LongRank3` – Exponentkoeffizienten, die die Gewichtskurve über das Lookback-Fenster formen.
- `LongShift1`, `LongShift2`, `LongShift3` – Zusätzliche Modifikatoren (0…1), die die Gewichte zum Anfang oder Ende des Fensters hin verschieben.
- `LongGap` – Maximale Größe des realen Kerzenkörpers, der den synthetischen Eröffnungspreis gleich dem vorherigen synthetischen Schlusskurs hält.
- `LongSignalBar` – Wie viele abgeschlossene Kerzen vor dem Lesen des Signals übersprungen werden (`0` wertet die letzte geschlossene Kerze aus, `1` die vorherige usw.).
- `EnableLongEntries` – Schaltet Long-Einstiege ein.
- `EnableLongExits` – Schaltet automatische Long-Exits ein.

### Short-Strom
- `ShortCandleType` – Kerzendatentyp für den Short-Indikatorstrom.
- `ShortLength`, `ShortRank1`, `ShortRank2`, `ShortRank3`, `ShortShift1`, `ShortShift2`, `ShortShift3`, `ShortGap`, `ShortSignalBar` – Identisch mit ihren Long-seitigen Gegenstücken, aber auf den Short-Strom angewendet.
- `EnableShortEntries` – Schaltet Short-Einstiege ein.
- `EnableShortExits` – Schaltet automatische Short-Exits ein.

### Handel
- `OrderVolume` – Basismenge für neue Positionen. Umkehrungen addieren automatisch die absolute aktuelle Position zu diesem Wert.
- `TakeProfitPoints` – Optionaler Take-Profit-Abstand in Preispunkten (0 deaktiviert ihn).
- `StopLossPoints` – Optionaler Stop-Loss-Abstand in Preispunkten (0 deaktiviert ihn).

## Hinweise
- Der ursprüngliche Expertenberater enthielt Geldmanagement-Modi basierend auf Balance oder Margin. Der Port exponiert einen einfacheren festen `OrderVolume`-Parameter. Passen Sie ihn auf das gewünschte Positionsgrößen-Sizing an.
- `StartProtection` wird nur aufgerufen, wenn das Instrument einen gültigen Preisschritt (`Security.Step > 0`) hat.
- Keine Python-Version ist absichtlich vorgesehen.
- Chart-Bereiche werden automatisch erstellt: wenn Long- und Short-Kerzenfeeds abweichen, werden zwei separate Panels angezeigt; andernfalls wird nur eines gezeigt.
- Die Strategie beruht auf abgeschlossenen Kerzen; sie reagiert nicht auf Intrabar-Updates.
