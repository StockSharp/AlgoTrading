# Graal EMA Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Umsetzung des MetaTrader 4 Expertenberaters **0Graal-CROSSmuvingi**. Es werden Trendumkehrungen gehandelt, die auftreten, wenn ein schneller exponentieller gleitender Durchschnitt (EMA) bei Schlusskursen einen langsameren EMA, der bei Eröffnungskursen berechnet wird, kreuzt. Ein Momentum-Oszillator bestätigt die Ausbruchsrichtung und ein Take-Profit mit fester Distanz reproduziert das ursprüngliche MT4-Ausführungsmodell.

## Handelsidee

1. **Fast EMA on close** verfolgt die letzte Preisbewegung.
2. **Langsames EMA beim Öffnen** hinkt hinterher und bildet die Crossover-Grundlinie.
3. **Momentum-Oszillator (Periode 14)** misst, wie stark der Preis vom neutralen Wert (100) weg beschleunigt. Die Strategie wird nur gehandelt, wenn das Momentum um mehr als einen konfigurierbaren Filter von 100 abweicht und in die gleiche Richtung weiter zunimmt.
4. **Take Profit** schließt Geschäfte nach einer vordefinierten Distanz, gemessen in Instrumentenpunkten, und spiegelt den MT4-Parameter `TakeProfit` wider.

## Teilnahmebedingungen

- **Lange Einrichtung**
  - Der schnelle EMA kreuzt den langsamen EMA der aktuellen fertigen Kerze, während der schnelle EMA im vorherigen Balken kleiner oder gleich dem langsamen EMA war.
  - Das Momentum (Wert minus 100) ist größer als der Schwellenwert `MomentumFilter` und auch höher als der Momentum-Wert des vorherigen Balkens.
  - Bestehende Short-Positionen werden geschlossen, bevor eine neue Long-Position eröffnet wird. Die neue Long-Größe entspricht dem konfigurierten `Volume` plus dem Betrag, der erforderlich ist, um einen offenen Short zu verkaufen.
- **Kurze Einrichtung**
  - Der schnelle EMA kreuzt den langsamen EMA, während im vorherigen Balken der schnelle EMA über oder gleich dem langsamen EMA lag.
  - Das Momentum (Wert minus 100) liegt unter dem negativen `MomentumFilter`-Schwellenwert und unter dem Momentum-Wert des vorherigen Balkens.
  - Bestehende Long-Positionen werden geschlossen, bevor eine neue Short-Position eröffnet wird. Die neue Short-Größe entspricht dem konfigurierten `Volume` plus der Menge, die zur Deckung einer offenen Long-Position erforderlich ist.

## Ausgangsregeln

- Positionen werden automatisch geschlossen, wenn der Preis das berechnete Take-Profit-Ziel (`TakeProfitPoints * PriceStep`) erreicht.
- Ein neues Gegensignal kehrt die Position ebenfalls sofort um, da die Ordergröße immer die Menge der aktuellen Position beinhaltet.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `FastPeriod` | Länge des EMA bei Schlusskursen. | 13 |
| `SlowPeriod` | Länge des EMA bei Eröffnungskursen. | 34 |
| `MomentumPeriod` | Rückblick auf den Impulsoszillator. | 14 |
| `MomentumFilter` | Minimale absolute Momentum-Abweichung von 100, die für den Handel erforderlich ist. | 0,1 |
| `TakeProfitPoints` | Abstand zum Gewinnziel in Preispunkten (multipliziert mit `PriceStep`). | 200 |
| `CandleType` | Für Berechnungen verwendeter Kerzendatentyp (standardmäßig 15-Minuten-Zeitrahmen). | 15-minütiger Zeitrahmen |
| `Volume` | Bestellgröße, die für Neueingaben verwendet wird. Die Engine erbt es von der Basisklasse. | 1 |

## Implementierungshinweise

- Signale werden nur bei geschlossenen Kerzen verarbeitet (`CandleStates.Finished`).
- Die Strategie abonniert den gewählten Kerzentyp mit `SubscribeCandles` und bindet sowohl EMA als auch Momentumindikatoren über den High-Level API.
- Der langsame EMA wird manuell mit Eröffnungspreisen innerhalb des Bind-Callbacks aktualisiert, um das MT4-Verhalten zu reproduzieren, bei dem `PRICE_OPEN` verwendet wurde.
- Das Take-Profit-Management überwacht die Intrabar-Hochs und -Tiefs, um die punktbasierte Exit-Logik von MT4 nachzuahmen.
- `StartProtection()` ist beim Start aktiviert, um vor unerwarteten offenen Positionen zu schützen, bevor die Strategie mit dem Handel beginnt.
