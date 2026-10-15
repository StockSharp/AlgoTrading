# PresentTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kausale ATR-Linie, gesteuert durch Schlusskurs-RSI oder MFI aus typischem Preis und Volumen. Bestätigte Kreuzungen mit dem zwei Kerzen verzögerten Linienwert eröffnen, kehren vollständig um oder schließen Positionen.

## Linie und Signale

Nur abgeschlossene Kerzen, standardmäßig fünf Minuten. Nativer Wilder-ATR und gewählter Oszillator teilen den positiven Zeitraum `Length=14`. Der ungewählte Oszillator wird nicht registriert. Beide gewählten Indikatoren müssen gebildet sein:

- Oszillator ≥50: Kandidat `low - Multiplier * ATR`; Linie = Maximum aus Kandidat und vorheriger Linie.
- Oszillator <50: Kandidat `high + Multiplier * ATR`; Linie = Minimum aus Kandidat und vorheriger Linie.
- Erster gebildeter Wert = gewählter Kandidat, ohne erfundene Vorgeschichte. `Multiplier=1`, positiv.

ATR umfasst Lücken zum vorherigen Schlusskurs, beginnt mit wachsendem Mittel und nutzt danach Wilder. RSI glättet Schlusskursgewinne/-verluste nach Wilder und behält bei unverändertem Schlusskurs den vorherigen Wert. MFI nutzt `typical=(high+low+close)/3` mal Volumen: steigend zählt positiv, fallend negativ, unverändert weder noch. Über die letzten `Length` Kerzen gilt `100 * positiveSum / (positiveSum + negativeSum)`, beziehungsweise 100 ohne negativen Fluss.

Ein rohes Longkreuz bei t verlangt `line[t-1] <= line[t-3]` und `line[t] > line[t-2]`; Short verwendet ≥ und <. Vier gebildete Linienwerte sind nötig.

Long wird nur bestätigt, wenn ein früheres rohes Shortsignal vorliegt, neuer als das vorherige rohe Longsignal oder bei noch fehlendem Longsignal. Short ist symmetrisch. Jedes rohe Signal wird nach dem Vergleich gespeichert, auch bei Handelsablehnung. Das erste Signal handelt nicht ohne beobachtetes Gegensignal. Wiederholungen derselben Richtung benötigen ein dazwischenliegendes Gegensignal.

## Ausführung und Standardwerte

Sind beide Richtungen aktiv, eröffnet `Volume` aus Null oder `Volume + abs(Position)` kehrt die Gegenposition vollständig um. Gleichgerichtete Signale halten. Nur Long: ein bestätigtes Shortsignal schließt den gesamten Long, ohne Short zu eröffnen; nur Short ist symmetrisch. Eine deaktivierte Richtung aus Null tut nichts. Ausstehende Orders blockieren Duplikate, während Linie und Signalhistorie weiterlaufen. Abgelehnte Signale werden nicht vorgemerkt; keine sechsstündige Wartezeit.

Keine gesonderten Preis-Stop-Loss-/Take-Profit-Regeln. Der einseitige Ausstieg erfolgt durch das bestätigte Gegensignal, nicht durch eine ATR-Preisstoporder.

| Parameter | Standard |
|---|---:|
| `Length` | 14, positive gemeinsame Periode |
| `Multiplier` | 1, positiver ATR-Abstand |
| `IsUseMfi` | false wählt RSI, true MFI |
| `IsLongEnabled` / `IsShortEnabled` | true / true; mindestens eine Richtung aktiv |
| `CandleType` | 5 Minuten |

C#-/Python-Tests berechnen ATR, RSI/MFI, Linie und rohe/bestätigte Kreuzungen unabhängig im vorhandenen BTC/TON-Archiv. Jede Order und tatsächliche Position wird nach Zeitpunkt, Richtung, vollständiger Umkehr oder einseitigem Schluss geprüft. Beide Oszillatoren und Signale mit weniger als sechs Stunden Abstand werden ausgeübt. Kein Rentabilitätsnachweis.
