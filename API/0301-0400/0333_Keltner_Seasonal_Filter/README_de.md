# Keltner Saisonaler Filter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt Keltner-Ausbrüche mit historischen Monatsrenditen als Filter. Auf abgeschlossenen 5-Minuten-Kerzen: `E = EMA(20)`, `A = ATR(14)`, `M = AtrMultiplier = 2`, `U = E + M × A`, `L = E − M × A`; `SeasonalThreshold = 0.5` Prozentpunkte. Indikatoren müssen gebildet und Handel erlaubt sein.

Kauf bei `S > SeasonalThreshold`, `Close > U`, `Position <= 0`; Verkauf bei `S < -SeasonalThreshold`, `Close < L`, `Position >= 0`. `Volume + abs(Position)` dreht die Gegenposition vollständig. Stop ab Signalschluss bei `Close − M × A` für Long oder `Close + M × A` für Short speichern.

Auf jeder abgeschlossenen Kerze zuerst den Stop prüfen: Long bei `Close <= stop`, Short bei `Close >= stop` schließen, Stop löschen und Kerzenverarbeitung beenden. Sonst Einstiege prüfen, danach verbliebenes Long unter E bzw. Short über E schließen. Manueller Stop am Kerzenschluss, ausgehend vom Signalschluss; kein börslicher Intrabar-Stop, Take-Profit oder Trailing.

Erfordert `SeasonalityHistoryFile`: eine UTF-8-CSV mit monatlichen Eröffnungs- und Schlusskursen und Kopfzeile `SecurityId,Month,Open,Close`. `Month` hat das Format `yyyy-MM`, Zahlen verwenden den Dezimalpunkt, Kurse müssen positiv und Monate je Handelsinstrument eindeutig sein. Andere Wertpapier-IDs werden ignoriert. Fehlende Daten, fehlerhafte Zeilen oder keine passenden Beobachtungen verhindern den Start.

Auf jeder Signalkerze ist `S` der Mittelwert von `100 × (Close / Open − 1)` für denselben Kalendermonat früherer Jahre innerhalb von `SeasonalityLookbackYears = 10`. Erforderlich sind mindestens `MinimumSeasonalSamples = 2` Beobachtungen. Aktuelle und zukünftige Monate werden auch dann ausgeschlossen, wenn sie in der Datei stehen. Ohne ausreichende Historie keine neuen Positionen. Tatsächliche Monatskurse des Instruments sind bereitzustellen; es gibt keine feste Monatstabelle.
