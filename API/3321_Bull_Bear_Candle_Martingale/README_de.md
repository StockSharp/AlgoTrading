# Bull-&-Bear-Candle-Martingal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie reagiert auf starke bullische und bärische Kerzen und eröffnet Marktpositionen in dieselbe Richtung. Sie nutzt eine unabhängige Martingal-Sequenz für jede Seite: Long-Positionen skalieren das Volumen mit dem *Bull Multiplier*, Short-Positionen verwenden den *Bear Multiplier*. Schutzdistanzen für Stop-Loss und Take-Profit werden ebenfalls getrennt je Richtung konfiguriert, was präzise Kontrolle über das asymmetrische Verhalten des ursprünglichen MQL-Expert-Advisors ermöglicht.

## Handelslogik
1. Den konfigurierten Kerzentyp abonnieren (Standard: 1 Minute) und nur auf abgeschlossene Kerzen warten.
2. Wenn keine Position offen ist:
   - **Bullisches Setup:** Wenn `Close > Open` und die Kerzenkörpergröße den bullischen Körperfilter überschreitet, zum Markt kaufen.
   - **Bärisches Setup:** Wenn `Close < Open` und die Körpergröße den bärischen Körperfilter überschreitet, zum Markt verkaufen.
3. Jeder Einstieg setzt Stop-Loss- und Take-Profit-Orders, die aus Pip-Distanzen in den Preisschritt des Instruments umgerechnet werden.
4. Wenn eine Position schließt, wird der realisierte PnL mit der vorherigen Basislinie verglichen:
   - Ein negatives Ergebnis multipliziert das jeweilige Martingal-Volumen.
   - Ein positives oder Break-even-Ergebnis setzt diese Seite auf das Anfangsvolumen zurück.
5. Neue Signale werden ignoriert, solange eine Position offen ist, wodurch das Single-Trade-Verhalten des Quell-EA reproduziert wird.

## Geldmanagement
- Long- und Short-Martingal-Zyklen werden unabhängig verfolgt, sodass eine verlierende Long-Serie den nächsten Short-Trade nicht beeinflusst und umgekehrt.
- Volumen werden an `VolumeStep` der Security ausgerichtet, um Orderablehnungen zu vermeiden.
- `StartProtection(useMarketOrders: true)` aktiviert StockSharps Schutzorder-Verwaltung für die angehängten Stop- und Take-Niveaus.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| **Initial Volume** | Basisvolumen, mit dem jeder Martingal-Zyklus für beide Richtungen startet. |
| **Bull Multiplier** | Multiplikator für den nächsten bullischen Trade nach einer verlierenden Long-Position. |
| **Bear Multiplier** | Multiplikator für den nächsten bärischen Trade nach einer verlierenden Short-Position. |
| **Bull Stop Loss** | Stop-Loss-Distanz in Pips für bullische Trades, in Preis über den Instrumentenschritt konvertiert. |
| **Bull Take Profit** | Take-Profit-Distanz in Pips für bullische Trades. |
| **Bear Stop Loss** | Stop-Loss-Distanz in Pips für bärische Trades. |
| **Bear Take Profit** | Take-Profit-Distanz in Pips für bärische Trades. |
| **Bull Body Filter** | Minimaler bullischer Kerzenkörper in Pips, der eine Kauforder auslöst. |
| **Bear Body Filter** | Minimaler bärischer Kerzenkörper in Pips, der eine Verkaufsorder auslöst. |
| **Candle Type** | Zeitrahmen für die Signalerzeugung (standardmäßig 1-Minuten-Zeitrahmen). |

## Nutzungshinweise
- Stellen Sie sicher, dass die verbundene Security gültige `PriceStep`- und `VolumeStep`-Werte bereitstellt. Die Strategie verwendet standardmäßig 0.0001, wenn `PriceStep` nicht geliefert wird.
- Die Martingal-Logik stützt sich auf realisierten PnL, daher aktualisiert auch manuelles Schließen von Positionen die Sequenz korrekt.
- Optimierung kann sich auf Körperfilter und Multiplikator-Kombinationen konzentrieren, um Reaktionsfähigkeit gegen Drawdown auszubalancieren.
