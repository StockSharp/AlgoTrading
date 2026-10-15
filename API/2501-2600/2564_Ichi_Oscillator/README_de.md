# Ichi-Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Konvertierung des MetaTrader-5-Expertenberaters **Exp_ICHI_OSC** in die StockSharp-High-Level-API.
- Handelt auf einer konfigurierbaren Kerzenserie und leitet Signale aus einem auf Ichimoku-Linien aufgebauten Oszillator ab.
- Der Rohoszillatorwert ist `((Close - SenkouA) - (Tenkan - Kijun)) / Step`, geglättet durch einen wählbaren gleitenden Durchschnitt.
- Orders werden mit dem Strategievolumen ausgeführt; komplexe Geldverwaltungsblöcke aus dem Originalcode wurden durch StockSharp-Positionsverwaltung ersetzt.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Kerzen-Zeitrahmen für alle Indikatorberechnungen. |
| `IchimokuBase` | Basisperiode, die die Längen von Tenkan (`base * 0.5`), Kijun (`base * 1.5`) und Senkou B (`base * 3`) definiert. |
| `Smoothing Method` | Gleitender Durchschnitt zur Glättung des Oszillators. Optionen: `Simple`, `Exponential`, `Smoothed`, `Weighted`, `Jurik`, `Kaufman`. |
| `Smoothing Length` | Periode der gewählten Glättungsmethode. |
| `Smoothing Phase` | Reservierter Kompatibilitätsparameter (aus der MQL-Version übernommen, derzeit nicht von den integrierten Glättungsimplementierungen verwendet). |
| `Signal Bar` | Anzahl der Balken zurück von der letzten fertigen Kerze, die zum Lesen von Oszillatorfarben verwendet wird (Standard `1`). |
| `Enable Buy Entries / Enable Sell Entries` | Öffnung von Long- bzw. Short-Positionen erlauben. |
| `Enable Buy Exits / Enable Sell Exits` | Schließen bestehender Long- oder Short-Positionen erlauben. |
| `Stop Loss (points)` | Schützende Stop-Distanz in Preisschritten. |
| `Take Profit (points)` | Take-Profit-Distanz in Preisschritten. |
| `Order Volume` | Basis-Ordervolumen für Marktorders. |

## Handelslogik
1. Die angeforderte Kerzenserie abonnieren und Tenkan-, Kijun- und Senkou-A-Werte mit den abgeleiteten Ichimoku-Perioden berechnen.
2. Den Oszillator aus den Differenzen zwischen Preis, Senkou A, Tenkan und Kijun aufbauen und durch den gewählten Smoother leiten.
3. Jedem geglätteten Wert einen Farbcode zuweisen:
   - `0` — Oszillator über null und steigend.
   - `1` — Oszillator über null und fallend.
   - `2` — neutral (Nulllevel oder unverändert).
   - `3` — Oszillator unter null und abnehmend.
   - `4` — Oszillator unter null und steigend.
4. Zwei Farben lesen: den Balken bei `SignalBar + 1` (vorherige Farbe) und den Balken bei `SignalBar` (aktuelle Farbe).
   - Wenn die vorherige Farbe `0` oder `3` ist, Shorts schließen wenn erlaubt und ein Long öffnen wenn die aktuelle Farbe `2`, `1` oder `4` ist.
   - Wenn die vorherige Farbe `4` oder `1` ist, Longs schließen wenn erlaubt und ein Short öffnen wenn die aktuelle Farbe `0`, `1` oder `3` ist.
5. Orders werden mit dem konfigurierten Volumen platziert. Longs und Shorts werden nie gestapelt: offene Signale werden erst ausgewertet, nachdem die Ausstiegslogik im selben Balken gelaufen ist.

## Risikomanagement
- Schutzorders werden über `StartProtection` verwaltet, mit Stop-Loss- und Take-Profit-Distanzen in Preisschritten.
- Kein Trailing oder partielle Ausstiege sind standardmäßig aktiviert.

## Hinweise
- Das ursprüngliche Geldverwaltungsmodul (Lot-Berechnungen, Abweichungsbehandlung, Handelstimer) wird durch StockSharp's Positions- und Volumenkontrolle ersetzt.
- Glättungsmethoden, die in StockSharp nicht existieren (z.B. JurX, ParMA, VIDYA, T3), sind nicht verfügbar; die nächstliegende Alternative aus der bereitgestellten Liste wählen.
- Signalzeitstempel in den Logs enthalten die Kerzenschlusszeit plus eine vollständige Kerzenperiode, was die Verwendung von `TimeShiftSec` in MQL widerspiegelt.
