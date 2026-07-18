# 5/8 MA-Kreuz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die 5/8 MA-Kreuz-Strategie ist ein StockSharp-Port des MetaTrader Expert Advisors "5_8 MACross". Sie vergleicht einen schnellen exponentiellen gleitenden Durchschnitt (EMA), der auf Schlusskursen berechnet wird, mit einem langsameren EMA, der auf Eröffnungskursen berechnet wird. Das System reagiert auf den Kreuzpunkt zwischen den beiden Durchschnitten und kann auf jedes Symbol und jeden Zeitrahmen angewendet werden, der standardmäßige zeitbasierte Kerzen bereitstellt.

## Indikatoren
- **Schneller EMA** – konfigurierbare Länge (Standard 5), berechnet vom Kerzenschlusskurs.
- **Langsamer EMA** – konfigurierbare Länge (Standard 8), berechnet vom Kerzeneröffnungskurs.

## Handelslogik
1. Die Strategie verarbeitet nur abgeschlossene Kerzen, um Teildaten zu vermeiden.
2. Ein Long-Einstieg wird generiert, wenn der schnelle EMA auf der vorherigen Kerze unter oder gleich dem langsamen EMA war und auf der aktuellen Kerze darüber kreuzt.
3. Ein Short-Einstieg wird generiert, wenn der schnelle EMA auf der vorherigen Kerze über oder gleich dem langsamen EMA war und auf der aktuellen Kerze darunter kreuzt.
4. Wenn ein Signal erscheint, kehrt die Strategie ihr Exposure um: Sie schließt jede offene Position und sendet eine Marktorder, die so dimensioniert ist, dass `Volume` Kontrakte in der neuen Richtung entstehen.

## Risikomanagement
- **Take-Profit** – optionales Ziel ausgedrückt in Preispunkten. Die Punktgröße wird aus dem Wertpapierpreisschritt abgeleitet; bei drei- und fünfstelligen Notierungen wird der Wert automatisch mit 10 multipliziert, um das Pip-Handling von MetaTrader zu emulieren.
- **Stop-Loss** – optionaler Schutzstop, ebenfalls ausgedrückt in Preispunkten vom Einstiegspreis.
- **Trailing-Stop** – optionaler Abstand in Preispunkten. Nach dem Öffnen einer Position verfolgt die Strategie das höchste Hoch (für Longs) oder das niedrigste Tief (für Shorts) und bewegt den Stop nur in der profitablen Richtung. Wenn kein anfänglicher Stop-Loss angegeben ist, initiiert der Trailing-Stop trotzdem sofort nach dem Einstieg Schutz.
- Wenn entweder der Take-Profit oder der (Trailing-)Stop bei einem Schlusskurs getroffen wird, wird die Position zum Markt geschlossen.

## Parameter
| Name | Beschreibung | Standardwerte |
| --- | --- | --- |
| `FastLength` | Periode des schnellen EMA (schlusskursbasiert). | 5 |
| `SlowLength` | Periode des langsamen EMA (eröffnungskursbasiert). | 8 |
| `TakeProfitPoints` | Take-Profit-Distanz in Preispunkten. | 40 |
| `StopLossPoints` | Stop-Loss-Distanz in Preispunkten (0 deaktiviert den Stop). | 0 |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Preispunkten (0 deaktiviert das Trailing). | 0 |
| `CandleType` | Für Berechnungen verwendeter Kerzentyp / Zeitrahmen. | 1-Minuten-Zeitrahmen |
| `Volume` | Ordervolumen aus der Basisklasse `Strategy`. | 0.1 |

## Unterschiede zur MQL-Version
- MetaTrader-spezifische Hedging-Prüfungen und Kontoinformationsaufrufe werden weggelassen, da StockSharp die Positionsbuchhaltung anders handhabt.
- Signale werden auf geschlossenen Kerzen ausgewertet und nicht auf dem allerersten Tick einer neuen Bar; dies verbessert die Stabilität in ereignisgesteuerten Umgebungen.
- Die Trailing-Logik verwendet das Kerzenhoch/-tief, um den Stop voranzubewegen, anstatt des aktuellen Bid/Ask-Ticks, was deterministisches Verhalten für die historische Verarbeitung bietet.

## Verwendungshinweise
- `Volume` in den Strategieeigenschaften konfigurieren, um die gewünschte Losgröße zu entsprechen.
- Die Strategie mit StockSharp-Schutzmodulen oder zusätzlichen Filtern kombinieren, wenn Risikomanagement auf Portfolio-Ebene erforderlich ist.
- Die Strategie platziert keine Pending Orders; alle Ein- und Ausstiege werden mit Marktorders ausgeführt, die durch die obige Kreuzungs- und Risikosteuerungslogik generiert werden.
