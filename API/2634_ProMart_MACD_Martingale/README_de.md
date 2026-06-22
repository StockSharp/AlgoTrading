# ProMart MACD Martingale-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des historischen MQL-Experten **MartGreg_1 / ProMart**. Sie kombiniert zwei MACD-Konfigurationen mit einem kontrollierten Martingale-Positionierungsmodell. Der primäre MACD sucht nach lokalen Tiefst- und Höchstwerten im Momentum, während der sekundäre MACD die Richtung der jüngsten Steigung bestätigt. Nach jedem geschlossenen Trade folgt die Strategie dem Indikatormuster erneut (wenn der letzte Trade profitabel war) oder dreht die Richtung sofort um (nach einem Verlust), während sie möglicherweise die nächste Ordergröße verdoppelt.

## Handelslogik

- **Signale**
  - Zwei MACD-Indikatoren auf der ausgewählten Kerzenserie aufbauen:
    - `MACD1` (schnell=5, langsam=20, Signal=3) fungiert als Musterdetektor.
    - `MACD2` (schnell=10, langsam=15, Signal=3) bestätigt die kurzfristige Steigung.
  - Signale nur auf abgeschlossenen Kerzen mit den vorherigen drei MACD1-Werten und den vorherigen zwei MACD2-Werten auswerten (spiegelt die MQL-Logik wider, die einen Bar zurückschaute).
  - **Long-Setup**: MACD1 bildet ein lokales Tal (`MACD1[t-1] > MACD1[t-2] < MACD1[t-3]`) und MACD2 steigt (`MACD2[t-2] > MACD2[t-1]`).
  - **Short-Setup**: MACD1 bildet einen lokalen Gipfel, während MACD2 fällt.
  - Wenn der letzte geschlossene Trade profitabel war, wartet die Strategie auf das nächste gültige Setup. Nach einem Verlusttrade öffnet sie sofort die Gegenrichtung, unabhängig von der aktuellen MACD-Form, und repliziert die ursprüngliche Martingale-Umkehr.
- **Positionsmanagement**
  - Trades werden mit Market-Orders geöffnet und auf jeder abgeschlossenen Kerze überwacht.
  - Stop-Loss- und Take-Profit-Niveaus werden in Preispunkten vom Einstiegspreis berechnet. Wenn das Hoch/Tief der Kerze eines der Niveaus erreicht, wird die Position zum Marktpreis geschlossen und das Trade-Ergebnis erfasst.
  - Kein neuer Trade wird auf derselben Kerze geöffnet, die eine Position geschlossen hat; die Strategie wartet auf den nächsten Bar, genau wie der MQL-Experte, der beim ersten Tick eines neuen Bars agierte.
- **Martingale-Sizing**
  - Ein Basisvolumen wird aus dem Portfolio-Eigenkapital dividiert durch `BalanceDivider` abgeleitet und auf den Volumen-Schritt des Instruments ausgerichtet (fällt auf die `Volume`-Eigenschaft oder das Instrument-Mindestvolumen zurück, wenn nötig).
  - Nach einem Verlusttrade kann die nächste Position das vorherige Ordervolumen verdoppeln, bis zu `MaxDoublingCount` mal hintereinander. Gewinn setzt den Verdopplungszähler zurück.
  - Volumen ist immer durch das maximale Instrument-Volumen begrenzt, um Überdimensionierung zu vermeiden.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `BalanceDivider` | Teiler, der auf das Portfolio-Eigenkapital zur Berechnung des Basis-Ordervolumens angewendet wird. | `1000` |
| `MaxDoublingCount` | Maximale Anzahl aufeinanderfolgender Volumenverdopplungen nach Verlusten. | `1` |
| `StopLossPoints` | Stop-Loss-Abstand in Preispunkten (`PriceStep * StopLossPoints`). | `500` |
| `TakeProfitPoints` | Take-Profit-Abstand in Preispunkten. | `1500` |
| `Macd1Fast` / `Macd1Slow` / `Macd1Signal` | Perioden für den primären MACD, der Täler/Gipfel erkennt. | `5 / 20 / 3` |
| `Macd2Fast` / `Macd2Slow` / `Macd2Signal` | Perioden für den sekundären MACD-Steigungsfilter. | `10 / 15 / 3` |
| `CandleType` | Datentyp der Kerzenserie (Standard: 1-Minuten-Zeitrahmen). | `TimeSpan.FromMinutes(1).TimeFrame()` |

## Hinweise

- Die Implementierung approximiert Intrabar-Stop-Loss- und Take-Profit-Füllungen mit Kerzenhochs und -tiefs, da das StockSharp-Beispiel auf abgeschlossenen Kerzen arbeitet.
- Das Positionsvolumen fällt auf die `Volume`-Eigenschaft der Strategie oder das Instrument-Mindestvolumen zurück, wenn Portfolio-Daten nicht verfügbar sind.
- Noch keine Python-Version vorhanden; nur die C#-Strategie ist enthalten.
- Die Konfiguration immer auf historischen Daten validieren, bevor der echte Handel aktiviert wird. Die Martingale-Komponente erhöht das Risiko erheblich.
