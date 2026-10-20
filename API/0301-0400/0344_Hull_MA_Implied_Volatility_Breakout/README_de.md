# Hull MA Implied Volatility Breakout

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

HMA = WMA(2 × WMA(Close, floor(HmaPeriod/2)) − WMA(Close, HmaPeriod), floor(sqrt(HmaPeriod))). Ein vollständiger aktueller und vorheriger HMA-Wert sind nötig. Aus flachem Bestand bei steigendem HMA kaufen, bei fallendem verkaufen, nur wenn beobachtete IV > vorheriger Mittelwert + IVMultiplier × σ. IV stammt in einheitlicher Einheit vom Optionsmarkt, nicht aus Kerzenspannen. Fallender HMA schließt Long, steigender Short auch bei fehlender oder normaler IV. Gleiche Werte geben kein Signal.

Mittelwert und Stichproben-Standardabweichung verwenden die vorherigen N aufeinanderfolgenden Beobachtungen ohne den aktuellen Wert: σ = sqrt(Σ(x − mean)² / (N − 1)). Das gesamte Fenster ist nötig. Es gibt keine Zufallswerte oder aus Preisen abgeleiteten Ersatzwerte.

## Eingabedaten

Die erforderliche CSV hat den Kopf SecurityId,Time,Value. Time ist der UTC-Eröffnungszeitpunkt der Signalkerze (yyyy-MM-ddTHH:mm:ssZ). Beobachtete Werte müssen endlich, nichtnegativ und je Zeitpunkt eindeutig sein. Nur die aktuelle Beobachtung gilt. Lücken leeren das Filterfenster und sperren Einstiege bis zur erneuten Initialisierung; Ausstiege bleiben aktiv.

## Parameter

| Parameter | Standardwert |
|---|---|
| `HmaPeriod` | `9` |
| `IVPeriod` | `20` |
| `IVMultiplier` | `2` |
| `IVHistoryFile` | erforderlich |
| `AtrPeriod` | `14` |
| `StopLossAtr` | `2` |
| `CandleType` | `TimeSpan.FromMinutes(15).TimeFrame()` |

## Ausführung

AtrPeriod = 14 verwendet Wilder-ATR. Beim Einstieg wird ATR(Signal) × StopLossAtr als Abstand fixiert. Lokaler nativer Marktschutz überwacht Quotes, verankert den Abstand am tatsächlichen Ausführungspreis und kann zwischen Signalkerzen schließen. Spätere ATR-Werte verschieben den Stop nicht. Null deaktiviert den Schutz für einen neuen Lauf. Ein Gegensignal schließt den gesamten Restbestand ohne sofortige Umkehr oder Aufstockung.

## Prüfung

Ausführungen werden ausschließlich mit dem vorhandenen BTC/TON-Archiv geprüft. Explizite Berichts-, Options- und Vorwärmdaten sind mechanische Testdaten, keine echten Unternehmens- oder Optionsdaten. Kürzere Fenster, zwei Gruppen und März als jährlicher Termin sind benannte Testprofile, kein Nachweis für unveränderte langfristige Standardwerte. Es wird keine Rendite behauptet.

