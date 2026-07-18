# Bago EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert den MetaTrader "Bago EA" Expert Advisor. Sie handelt Trendfolge-Ausbrüche, die durch gleitende
Durchschnitts- und RSI-Kreuzungen bestätigt werden, während der Vegas-Tunnel (144/169-EMA-Paar) räumliche Filter und
Trailing-Anker bereitstellt.

## Handelslogik

1. **Indikatorvorbereitung**
   - Zwei EMAs (Perioden `FastPeriod` und `SlowPeriod`, Methode `MaMethod`, Preis `MaAppliedPrice`).
   - Vegas-Tunnel-EMAs (Perioden 144 und 169, gleiche Methode/Preis) zur Erkennung des Richtungskanals.
   - RSI (`RsiPeriod`, `RsiAppliedPrice`) zur Bestätigung.
   - Alle Preis-zu-Pip-Umrechnungen verwenden den Instrument-`PriceStep` mit 3/5-stelliger Anpassung wie der Original-EA.
2. **Kreuzungs-Zustandsmaschine**
   - EMA-Kreuzung auf/ab und RSI-Kreuzung über/unter 50 werden mit Timern verfolgt. Jeder Zustand bleibt für
     `CrossEffectiveBars` Kerzen aktiv und wird durch die entgegengesetzte Kreuzung oder den Timeout zurückgesetzt.
   - Tunnel-Kreuzungen markieren, wenn der Preis von einer Seite des Vegas-Tunnels zur anderen wechselt.
3. **Eintrittsbedingungen**
   - **Long**: Sowohl EMA- als auch RSI-Kreuzung aufwärts sind aktiv *und* Preis:
     - Schließt über dem Tunnel um mindestens `TunnelBandWidthPips` aber nicht weiter als `TunnelSafeZonePips`, mit
       bullischem Kerzenkörper, oder
     - Schließt unter dem Tunnel um `TunnelBandWidthPips`, was einen Rückprall von unten signalisiert.
   - **Short**: Spiegellogik mit EMA/RSI-Kreuzungen nach unten.
   - Handel ist nur innerhalb aktivierter Sessions erlaubt (London 07–16, New York 12–21, Tokio 00–08, oder jede Bar die
     nach 23:00 schließt).
4. **Orderverarbeitung**
   - Neue Positionen werden mit Volumen `TradeVolume` geöffnet. Gegensätzliche Positionen werden vor dem Umkehren geschlossen.
   - Anfangs-Stop wird bei `StopLossPips` vom Schlusskurs gesetzt. Stop-zu-Tunnel-Offsets verwenden `StopLossToFiboPips`.
5. **Trailing und Teilausstiege**
   - Wenn der Preis über Vegas-Tunnel-Niveaus hinausschreitet, bewegt sich der Stop:
     - Innerhalb des Tunnels parkt der Stop bei `tunnel ± (TrailingStepX + StopLossToFibo)`.
     - Außerhalb des Tunnels wird ein harter Trailing von `TrailingStopPips` hinter dem Preis angewendet.
   - Teilausstiege schließen `PartialClose1Volume` bei `TrailingStep1Pips` und `PartialClose2Volume` bei `TrailingStep2Pips`,
     sobald der Preis weit genug vom Einstieg entfernt ist.
   - Eine entgegengesetzte EMA/RSI-Kreuzung schließt die gesamte Position sofort.
6. **Stops**
   - Schutzaufträge werden als Markt-Stop-Aufträge geführt. Sie werden storniert, sobald die Position geschlossen wird.

## Parameter

| Parameter | Typ | Standard | Beschreibung |
|-----------|-----|----------|--------------|
| `TradeVolume` | decimal | 3 | Ordergröße in Lots. |
| `StopLossPips` | decimal | 30 | Anfangs-Stop-Loss-Abstand. |
| `StopLossToFiboPips` | decimal | 20 | Zusätzlicher Puffer beim Parken von Stops um den Vegas-Tunnel. |
| `TrailingStopPips` | decimal | 30 | Abstand des Trailing Stops, sobald der Preis den Tunnel verlässt. |
| `TrailingStep1Pips` | decimal | 55 | Erste Gewinnschicht für Teilausstieg und Stop-Verlagerung. |
| `TrailingStep2Pips` | decimal | 89 | Zweite Gewinnschicht für Teilausstieg und Trailing. |
| `TrailingStep3Pips` | decimal | 144 | Letzte Schicht vor reinem Trailing. |
| `PartialClose1Volume` | decimal | 1 | Volumen bei `TrailingStep1Pips` geschlossen. |
| `PartialClose2Volume` | decimal | 1 | Volumen bei `TrailingStep2Pips` geschlossen. |
| `CrossEffectiveBars` | int | 2 | Anzahl Bars, für die EMA/RSI-Kreuzungen gültig bleiben. |
| `TunnelBandWidthPips` | decimal | 5 | Neutrale Zone um den Vegas-Tunnel, in der keine Trades eingegangen werden. |
| `TunnelSafeZonePips` | decimal | 120 | Maximaler Abstand über dem Tunnel für Long-Einträge (und unter dem Tunnel für Shorts). |
| `EnableLondonSession` | bool | true | Signale während der London-Stunden erlauben. |
| `EnableNewYorkSession` | bool | true | Signale während der New-York-Stunden erlauben. |
| `EnableTokyoSession` | bool | false | Signale während der Tokio-Stunden erlauben. |
| `FastPeriod` | int | 5 | Schnelle EMA-Länge. |
| `SlowPeriod` | int | 12 | Langsame EMA-Länge. |
| `MaShift` | int | 0 | Horizontale Verschiebung auf alle EMAs angewendet. |
| `MaMethod` | `MovingAverageType` | Exponential | Glättungsmodus des gleitenden Durchschnitts. |
| `MaAppliedPrice` | `AppliedPriceType` | Close | Kerzenpreis weitergeleitet an die EMAs. |
| `RsiPeriod` | int | 21 | RSI-Mittelungslänge. |
| `RsiAppliedPrice` | `AppliedPriceType` | Close | Kerzenpreis weitergeleitet an den RSI. |
| `CandleType` | `DataType` | H1-Zeitrahmen | Kerzenreihe für die Berechnung. |

## Hinweise

- Die Strategie hält Indikator-Zustände auch außerhalb der Handelszeiten, genau wie im Original-EA.
- Stop-Aufträge werden über die High-Level-API (`SellStop`/`BuyStop`) verwaltet, um MetaTrader `PositionModify`-Aufrufe
  nachzuahmen.
- Alle Kommentare und Strukturen folgen den Repository-Richtlinien (Tabs für Einrückung, englische Inline-Kommentare).
