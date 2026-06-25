# ColorJjrsxTimePlus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert aus dem MetaTrader5-Experten `Exp_ColorJJRSX_Tm_Plus`. Die Strategie handelt Trendumkehrungen, die mit einem Jurik-geglätteten RSI-Oszillator erkannt werden, und enthält optionale zeitbasierte Exits, die die ursprünglichen Geldverwaltungs-Toggles imitieren.

## Übersicht

- **Idee**: Den Steigungsverlauf des Color-JJRSX-Oszillators verfolgen (über RSI geglättet mit einem Jurik Moving Average approximiert). Wenn der Oszillator nach oben dreht, kann das System Shorts schließen und optional Longs eröffnen, und umgekehrt bei Abwärtsbewegungen.
- **Markt**: Einzelnes Instrument, definiert durch das verbundene `Security`.
- **Zeitrahmen**: Konfigurierbar; Standard sind 4-Stunden-Kerzen (entspricht der ursprünglichen EA-Eingabe).
- **Richtung**: Long und Short. Jede Richtung kann unabhängig deaktiviert werden.
- **Ordertyp**: Marktorders über `BuyMarket()` / `SellMarket()`.

## Indikator-Stack

1. **Relative Strength Index (RSI)** — Basis-Momentum-Oszillator mit dem Parameter `RSI Length` (spiegelt `JurXPeriod` wider).
2. **Jurik Moving Average (JMA)** — Glättet die RSI-Ausgabe mit `Smoothing Length` (spiegelt `JMAPeriod` wider). Der JMA-Phasenparameter der MQL-Version ist in StockSharp nicht verfügbar und wird daher weggelassen.
3. **Signal Shift** — Reproduziert den `SignalBar`-Parameter. Signale werden aus dem Wert `Signal Shift` Balken zurück und den zwei vorhergehenden Werten generiert, um Steigungsänderungen zu erkennen.

## Handelslogik

### Long-Management
- **Einstieg**: Aktiviert durch `Enable Long Entries`. Erfordert, dass der geglättete Oszillator vor zwei Balken rückläufig war (`previous > older` ist falsch), auf dem letzten abgeschlossenen Balken nach oben drehte (`previous < older`) und auf dem aktuellen Balken weiter steigt (`current > previous`). Position muss flach oder short sein.
- **Ausstieg**: Wenn `Exit Long on Downturn` aktiviert ist und der Oszillator nach unten neigt (`previous > older`), wird ein offener Long geschlossen.

### Short-Management
- **Einstieg**: Aktiviert durch `Enable Short Entries`. Erfordert, dass der Oszillator nach unten dreht (`previous > older`) und auf dem aktuellen Balken weiter fällt (`current < previous`), während die Strategie flach oder long ist.
- **Ausstieg**: Wenn `Exit Short on Upturn` aktiviert ist und der Oszillator nach oben neigt (`previous < older`), wird ein offener Short gedeckt.

### Zeitfilter
- `Enable Time Exit` schließt Positionen, sobald ihre Haltezeit `Holding Minutes` überschreitet. Dies spiegelt den Timer des ursprünglichen Experten wider, der Positionen nach `nTime` Minuten liquidiert.

### Risikokontrollen
- `Stop Loss (pts)` und `Take Profit (pts)` werden über `StartProtection` mit `UnitTypes.PriceStep` in StockSharp-Schutzlevel umgewandelt.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `Indicator Timeframe` | Kerzentyp für die Indikatorberechnungen. | 4-Stunden-Kerzen |
| `RSI Length` | Periode für den RSI (analog zum JurX-Periode). | 8 |
| `Smoothing Length` | Länge des Jurik-MA-Glättens (analog zur JMA-Periode). | 3 |
| `Signal Shift` | Anzahl der abgeschlossenen Balken, die vor der Steigungsprüfung übersprungen werden (`SignalBar`). | 1 |
| `Enable Long Entries` / `Enable Short Entries` | Öffnen von Trades in jede Richtung erlauben. | true |
| `Exit Long on Downturn` / `Exit Short on Upturn` | Oszillator-gesteuerte Exits für bestehende Positionen erlauben. | true |
| `Enable Time Exit` | Haltezeitbasierte Liquidation aktivieren. | true |
| `Holding Minutes` | Maximale Minuten, eine Position offen zu halten. | 240 |
| `Stop Loss (pts)` | Abstand des Schutz-Stops in Preisschritten. | 1000 |
| `Take Profit (pts)` | Abstand des Gewinnziels in Preisschritten. | 2000 |

## Hinweise zur Konvertierung

- Der JJRSX-Histogramm-Puffer des ursprünglichen Indikators wird mit RSI + Jurik-Glättung emuliert. Es wird nur Steigungsinformation verwendet, sodass numerische Skalenunterschiede die Entscheidungen nicht beeinflussen.
- Geldverwaltungsoptionen (`MM`, `MMMode`, `Deviation`) sind nicht portiert. Die StockSharp-Order-Sizing sollte über die `Strategy.Volume`-Eigenschaft oder externe Portfolio-Einstellungen gehandhabt werden.
- Globale Variablen, die in MQL zur Ratenbegrenzung von Orders verwendet werden, sind hier unnötig, da die Strategie nur auf fertige Kerzen reagiert.
- Alle Kommentare und Dokumentation sind gemäß Repository-Richtlinien auf Englisch.
