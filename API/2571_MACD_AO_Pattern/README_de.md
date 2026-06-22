# MACD AO Muster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist eine originalgetreue StockSharp-Portierung des FORTRADER-Expertenberaters `MACD.mq5`. Sie implementiert das „AOP"-Muster, das den MACD-Oszillator auf tiefe Exkursionen von der Nulllinie beobachtet, gefolgt von einem Rückhaken in Richtung Neutralität. Wenn der Haken bestätigt wird, tritt die Strategie in Richtung der erwarteten Umkehr ein und wendet sofort feste Stop-Loss- und Take-Profit-Ziele in Pips an.

## Strategie-Logik
### Datenvorbereitung
- Arbeitet auf der Kerzenserie, die durch den Parameter `CandleType` ausgewählt wurde (standardmäßig 5-Minuten-Kerzen).
- Verwendet einen Standard-MACD-Indikator mit konfigurierbaren schnellen, langsamen und Signal-Perioden (Standardwerte 12/26/9).
- Speichert die MACD-Hauptlinienwerte der drei zuletzt abgeschlossenen Kerzen, um den indexbasierten MQL-Zugriff zu reproduzieren (`iMACD(...,1..3)`).

### Short-Setup (bärischer Haken)
1. **Aktivierung** – sobald die MACD-Hauptlinie der zuletzt geschlossenen Kerze unter `BearishExtremeLevel` fällt (Standard −0.0015), beginnt die Strategie, auf eine Umkehr zu achten.
2. **Neutraler Rückzug** – wenn MACD wieder über `BearishNeutralLevel` steigt (Standard −0.0005), wird die Hakenvalidierungsphase aktiv.
3. **Hakenbestätigung** – die drei vorherigen MACD-Werte müssen ein lokales Maximum bilden (`macd₁ < macd₂ > macd₃`), während der aktuellste Wert unterhalb des Neutralniveaus bleibt und der ältere Wert darüber. Dies reproduziert das ursprüngliche Muster, das sicherstellt, dass der Momentum nachlässt.
4. **Einstieg** – wenn keine Long-Position offen ist (`Position <= 0`), wird eine Marktverkaufsorder von `OrderVolume` gesendet. Schutzlevel werden sofort berechnet: Stop-Loss über dem Einstieg um `StopLossPips` und Take-Profit darunter um `TakeProfitPips` (in Preis umgewandelt durch `GetPipSize`).
5. Jede positive MACD-Lesung bricht das Setup ab und setzt die interne bärische Zustandsmaschine zurück, bis ein neuer tiefer negativer Abschnitt erscheint.

### Long-Setup (bullischer Haken)
1. **Aktivierung** – sobald MACD über `BullishExtremeLevel` steigt (Standard +0.0015), wird der bullische Beobachtungsmodus aktiviert.
2. **Sofortige Stornierung** – wenn MACD unter null fällt, wird das bullische Szenario aufgegeben und spiegelt die MQL-Logik wider.
3. **Neutraler Rückzug** – ein Rückfall unter `BullishNeutralLevel` (Standard +0.0005) bereitet die Hakenbestätigung vor.
4. **Hakenbestätigung** – die drei gespeicherten MACD-Werte müssen ein lokales Minimum bilden (`macd₁ > macd₂ < macd₃`) unter Beachtung der neutralen Schwellenwerte.
5. **Einstieg** – wenn keine Short-Position vorhanden ist (`Position >= 0`), kauft die Strategie zum Marktpreis mit `OrderVolume` und setzt Stop-Loss und Take-Profit symmetrisch zu den Short-Regeln.

### Risikomanagement
- Stop-Loss und Take-Profit sind immer über `_stopPrice` und `_takePrice` aktiv. Sie werden bei jeder abgeschlossenen Kerze mit dem aufgezeichneten Hoch/Tief ausgewertet, um die brokerseitige Ausführung im Original-EA zu emulieren.
- Pips werden mit `Security.PriceStep` in absolute Preise umgerechnet. Für 3- und 5-stellige FX-Symbole wird der Schritt mit 10 multipliziert, um die MQL-Anpassung für Bruchteils-Pips anzupassen.
- Wenn die Strategie eine Position wegen der Schutzlevel verlässt, löscht sie diese sofort und wartet auf ein neues Setup bei den nächsten Kerzen.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `CandleType` | Von der Strategie verarbeitete Kerzendatenserie. | 5-Minuten-Zeitrahmen |
| `OrderVolume` | Mit jeder Marktorder eingereichte Menge. | 0.1 |
| `TakeProfitPips` | Entfernung zum Gewinnziel in Pips. Für Optimierung markiert. | 60 |
| `StopLossPips` | Entfernung zum Stop-Loss in Pips. Für Optimierung markiert. | 70 |
| `MacdFastPeriod` | Schnelle EMA-Länge für MACD. | 12 |
| `MacdSlowPeriod` | Langsame EMA-Länge für MACD. | 26 |
| `MacdSignalPeriod` | Signal-EMA-Länge für MACD. | 9 |
| `BearishExtremeLevel` | Negativer MACD-Schwellenwert, der Short-Chancen aktiviert. | −0.0015 |
| `BearishNeutralLevel` | Negativer MACD-Schwellenwert zur Validierung des bärischen Hakens. | −0.0005 |
| `BullishExtremeLevel` | Positiver MACD-Schwellenwert, der Long-Chancen aktiviert. | +0.0015 |
| `BullishNeutralLevel` | Positiver MACD-Schwellenwert zur Validierung des bullischen Hakens. | +0.0005 |

## Zusätzliche Hinweise
- Die Strategie reagiert nur einmal pro abgeschlossener Kerze und ahmt den ursprünglichen `PrevBars`-Schutz in MQL nach.
- Die Stop-Loss/Take-Profit-Verwaltung ist rein preisbasiert; es gibt keine Trailing-Anpassungen oder Wiedereinstiege, bis der vollständige Zustandsmaschinenzyklus erneut durchläuft.
- Für Hedging-Konten im Quell-EA konzipiert, aber dieser Port erzwingt eine einzige Nettoposition durch Überprüfung von `Position` vor dem Senden neuer Orders.
- Keine Python-Version vorhanden, wie angefordert.
