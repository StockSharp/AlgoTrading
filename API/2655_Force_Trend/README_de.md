# Force-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des MetaTrader-5-Expertenberaters **Exp_ForceTrend.mq5** in `MQL/18817`.
- Verwendet den proprietären ForceTrend-Oszillator, um Übergänge zwischen bullischem und bärischem Momentum zu erkennen.
- Implementiert die Logik mit der High-Level-API von StockSharp unter Verwendung von Kerzenabonnements und integrierten Indikatoren statt direktem Serienzugriff.

## ForceTrend-Indikator
- Der Indikator schaut `Length` Kerzen zurück und misst den Abstand zwischen dem höchsten Hoch und dem tiefsten Tief innerhalb dieses Fensters.
- Der Mittelpreis der aktuellen Kerze wird innerhalb dieser Spanne normalisiert und zweimal geglättet:
  - Die erste Stufe erzeugt einen intermediären `force`-Wert mit den Koeffizienten `0.66` und `0.67`.
  - Die zweite Stufe wendet eine logarithmische Transformation kombiniert mit Halbwertszeit-Glättung an, um den endgültigen ForceTrend-Wert zu erhalten.
- Werte über null werden als bullisch behandelt (ursprünglich blau dargestellt) und Werte unter null sind bärisch (magenta dargestellt).

## Parameter
- `Length` – Größe des ForceTrend-Lookback-Fensters; muss positiv bleiben.
- `SignalBar` – wie viele abgeschlossene Kerzen das Signal verschoben wird. `0` reagiert auf die neueste geschlossene Kerze, `1` ahmt die MT5-Standardeinstellung nach, indem eine extra Kerze abgewartet wird, und höhere Werte verzögern die Ausführung noch mehr.
- `EnableLongEntry` – wenn deaktiviert, öffnet die Strategie keine Long-Positionen bei bullischen Übergängen.
- `EnableShortEntry` – wenn deaktiviert, öffnet die Strategie keine Short-Positionen bei bärischen Übergängen.
- `EnableLongExit` – steuert, ob bullische Signale vorhandene Short-Positionen schließen dürfen.
- `EnableShortExit` – steuert, ob bärische Signale vorhandene Long-Positionen schließen dürfen.
- `CandleType` – Zeitrahmen der für Indikatorberechnungen verwendeten Kerzen.

## Handelsregeln
1. Die ForceTrend-Ausgabe wird in eine diskrete Richtung (`+1`, `0`, `-1`) konvertiert.
2. Richtungen werden in einem Verlauf fester Länge gespeichert, damit die Strategie die Kerze beim `SignalBar`-Offset mit der unmittelbar vorherigen Kerze vergleichen kann.
3. Ein bullisches Signal (`direction > 0`) löst aus:
   - Schließen offener Short-Positionen, wenn `EnableShortExit` `true` ist.
   - Eröffnen oder Umkehren in eine Long-Position (Marktorder in Größe `Volume + |Position|`), wenn die vorherige Richtung nicht bullisch war und `EnableLongEntry` `true` ist.
4. Ein bärisches Signal (`direction < 0`) löst die symmetrischen Aktionen für Long-Positionen aus, wenn `EnableLongExit`/`EnableShortEntry` aktiviert sind.
5. Neutrale ForceTrend-Lesungen erben die zuletzt bekannte Richtung, damit das System nicht zwischen Flat-Zuständen oszilliert.
6. Orders werden nur gesendet, wenn die Strategie vollständig geformt, online und der Handel durch die StockSharp-Runtime erlaubt ist.

## Implementierungshinweise
- Kerzen werden über `SubscribeCandles(CandleType)` empfangen; die Indikatorverarbeitung erfolgt im `ProcessCandle`-Callback.
- Höchst- und Tiefstkurse werden über StockSharp-Indikatoren `Highest` und `Lowest` ermittelt, sodass keine manuelle Pufferverwaltung oder LINQ-Operationen erforderlich sind.
- Der Richtungsverlauf wird in einem kleinen festen Array gespeichert, das entsprechend `SignalBar` dimensioniert ist, um das ursprüngliche MT5-Verhalten ohne Neuerstellung von Collections für jeden Tick zu reproduzieren.
- Positionsumkehrungen verwenden eine einzelne Marktorder mit einem Volumen, das der Summe aus dem gewünschten Exposure und der absoluten aktuellen Position entspricht, und ahmt die `BuyPositionOpen`/`SellPositionOpen`-Helfer der MQL-Version nach.
- Geldverwaltungsparameter des Expertenberaters (Lot-Dimensionierung, Stop-Loss und Take-Profit in Punkten) werden bewusst weggelassen; die StockSharp-Strategie verlässt sich auf das vom Benutzer konfigurierte `Volume` und optionale externe Schutzmodule.
- Die booleschen Schalter spiegeln die MT5-Eingaben wider (`BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose`).

## Verwendungshinweise
- Die `Volume`-Eigenschaft vor dem Start der Strategie konfigurieren, um die Ordergröße zu steuern.
- Einen Kerzentyp wählen, der dem bei MT5-Tests verwendeten Zeitrahmen entspricht (Standard: 4-Stunden-Kerzen).
- Mit StockSharp-Risiko-/Schutzkomponenten kombinieren, wenn Stop-Loss- oder Take-Profit-Automatisierung erforderlich ist.

## Dateien
- Strategieimplementierung: `CS/ForceTrendStrategy.cs`
- Ursprüngliche MQL-Dateien: `MQL/18817/mql5/Experts/Exp_ForceTrend.mq5` und `MQL/18817/mql5/Indicators/ForceTrend.mq5`
