# Strategie zur Kreuzenden Gleitenden Durchschnitt
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des MetaTrader 5-Expertenberaters **"Crossing Moving Average (barabashkakvn's edition)"** aus dem Quellcode `MQL/21515`.
- Implementiert die Logik auf der StockSharp-High-Level-API mit Kerzen-Abonnements und Indikator-Bindung.
- Entwickelt für Instrumente, bei denen Momentum und gleitende Durchschnittskruzungen Trendumkehrungen erfassen.
- Dieses Paket enthält nur die C#-Version. Eine Python-Übersetzung wird gemäß Anfrage absichtlich weggelassen.

## Kernidee
Die Strategie überwacht zwei konfigurierbare gleitende Durchschnitte (schnell und langsam) mit optionalen Vorwärtsverschiebungen und kombiniert ihre Kreuzung mit einem Momentum-Bestätigungsfilter. Ein Trade wird nur geöffnet, wenn:
1. Der schnelle Durchschnitt den langsamen Durchschnitt um mindestens die konfigurierte Mindestdistanz (in Pips) über die zwei jüngsten abgeschlossenen Balken kreuzt.
2. Der Momentum-Indikator über (für Long) oder unter (für Short) den benutzerdefinierten Schwellenwert steigt und sich in Richtung des Trades verbessert.
3. Die Signalpreisquelle kann zwischen Eröffnung, Hoch, Tief, Schluss, Median, Typisch oder gewichteten Kerzenpreisen gewählt werden, um MetaTrader-Angewandte-Preis-Modi nachzuahmen.

## Risiko- und Trade-Management
- Das **Ordervolumen** ist pro Trade fest und wird sowohl beim Einsteigen in eine neue Position als auch beim Umkehren einer bestehenden Position angewendet.
- **Stop-Loss/Take-Profit**-Abstände werden in Pips konfiguriert und automatisch in Preisoffsets unter Verwendung von `Security.PriceStep` umgewandelt. Für Instrumente mit 3 oder 5 Dezimalstellen multipliziert die Strategie den Schritt mit 10, um die MetaTrader-Pip-Größe zu reproduzieren.
- Der **Trailing-Stop** aktiviert sich, nachdem sich der Preis um `TrailingStop + TrailingStep` (in Pips) vom Einstieg bewegt. Einmal ausgelöst, wird der Stop auf `aktueller Preis - TrailingStop` für Long-Positionen (oder `aktueller Preis + TrailingStop` für Shorts) bewegt, wann immer er um mindestens `TrailingStep` Pips vorrücken kann.
- Schutzlevels werden bei jeder fertigen Kerze ausgewertet: Wenn der Kerzenbereich den Stop-Loss oder Take-Profit berührt, wird die Position zum Markt geschlossen, um die Orderausführung in MetaTrader nachzuahmen.

## Indikatoren
- **Schneller Gleitender Durchschnitt** – konfigurierbarer Zeitraum, Verschiebung und Glättungsmethode (SMA, EMA, SMMA, WMA).
- **Langsamer Gleitender Durchschnitt** – gleiche Optionen wie der schnelle MA.
- **Momentum** – Zeitraum und Preisquelle identisch mit den gleitenden Durchschnitten. Die Strategie erkennt automatisch, ob der Indikator Werte um 0 oder 100 ausgibt, und wendet den Filter entsprechend an.

## Signallogik
1. Warten, bis alle Indikatoren vollständig gebildet sind. Der Algorithmus führt eine interne Geschichte der neuesten Werte, um verschobene Kreuzungen genau wie im ursprünglichen Expertenberater auszuwerten.
2. Die Preisdistanz zwischen dem schnellen und langsamen Durchschnitt auf den zwei vorherigen Balken (mit angewendeten Verschiebungen) berechnen. Die schnelle Linie muss die langsame Linie kreuzen und den Mindestdistanzfilter überschreiten.
3. Momentum-Werte auf denselben Balken abrufen. Für Long-Einstiege muss das aktuelle Momentum größer als sowohl der konfigurierte Schwellenwert als auch der vorherige Momentum-Wert sein; für Short-Einstiege ist das Gegenteil erforderlich.
4. Wenn ein neues Signal erscheint, während die Position entgegengesetzt ist, schließt die Strategie die bestehende Position und öffnet sofort eine in der neuen Richtung mit der konfigurierten Losgröße.

## Parameterreferenz
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `OrderVolume` | Basisvolumen für jede Marktorder. | `1` |
| `StopLossPips` | Stop-Loss-Abstand in Pips (0 deaktiviert den Stop). | `50` |
| `TakeProfitPips` | Take-Profit-Abstand in Pips (0 deaktiviert das Ziel). | `50` |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips (0 deaktiviert Trailing). | `5` |
| `TrailingStepPips` | Mindest-Pip-Verbesserung zum Bewegen des Trailing-Stops. | `5` |
| `MinDistancePips` | Mindestabstand zwischen MAs zur Validierung der Kreuzung. | `0` |
| `MomentumFilter` | Mindestmomentum-Differenz für Einstiege. | `0.1` |
| `FastPeriod` / `FastShift` | Länge des schnellen MA und horizontale Verschiebung (Balken). | `13` / `1` |
| `SlowPeriod` / `SlowShift` | Länge des langsamen MA und horizontale Verschiebung (Balken). | `34` / `3` |
| `MaMethod` | Glättungstyp (Simple, Exponential, Smoothed, Weighted). | `Exponential` |
| `AppliedPrice` | Kerzenpreis für Indikatorberechnungen. | `Close` |
| `MomentumPeriod` | Rückblicklänge des Momentums in Balken. | `14` |
| `CandleType` | Datenkurzentyp für die Strategie. | `TimeFrame(1m)` |

## Praktische Hinweise
- Immer sicherstellen, dass `Security.PriceStep` für das Instrument konfiguriert ist; andernfalls fällt das pip-basierte Risikomanagement auf rohe Preiseinheiten zurück.
- Die Trailing-Logik erfordert ein positives `TrailingStepPips`, wenn `TrailingStopPips` aktiviert ist—spiegelt die ursprüngliche MetaTrader-Validierung wider.
- Da Stop- und Take-Levels auf Kerzenbereichen ausgewertet werden, bietet die Verwendung hochauflösenderer Kerzen eine genauere Annäherung an tick-basierte Ausführung.
- Protokollierungsmeldungen bei Einstiegen und Trailing-Anpassungen sind enthalten, um Debugging und Parameteroptimierung zu erleichtern.
