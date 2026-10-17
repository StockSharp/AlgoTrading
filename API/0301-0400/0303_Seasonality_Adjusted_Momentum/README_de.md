# Saisonalitätsbereinigter-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verbindet Momentum mit historischen Monatsrenditen. Auf abgeschlossenen 5-Minuten-Kerzen werden `M = Momentum(14)` und `A = SMA(14)` von M berechnet. Ohne Position: Kauf bei `S >= SeasonalityThreshold` und `M > A`, Verkauf bei `S <= -SeasonalityThreshold` und `M < A`. Standard `SeasonalityThreshold = 0.5`, in Prozentpunkten.

Long schließen, wenn die saisonale Bedingung entfällt oder `M < A`; Short schließen, wenn dessen Bedingung entfällt oder `M > A`. Jeder Signalauftrag startet `CooldownBars = 120` abgeschlossene Kerzen Pause für weitere Signalentscheidungen. Ausgeführte Positionen erhalten getrennten lokalen nativen Schutz `StopLossPercent = 2` ab dem tatsächlichen Ausführungskurs, mit Bid/Ask-Überwachung und Marktausstieg unabhängig vom Cooldown.

Erfordert `SeasonalityHistoryFile`: eine UTF-8-CSV mit monatlichen Eröffnungs- und Schlusskursen und Kopfzeile `SecurityId,Month,Open,Close`. `Month` hat das Format `yyyy-MM`, Zahlen verwenden den Dezimalpunkt, Kurse müssen positiv und Monate je Handelsinstrument eindeutig sein. Andere Wertpapier-IDs werden ignoriert. Fehlende Daten, fehlerhafte Zeilen oder keine passenden Beobachtungen verhindern den Start.

Auf jeder Signalkerze ist `S` der Mittelwert von `100 × (Close / Open − 1)` für denselben Kalendermonat früherer Jahre innerhalb von `SeasonalityLookbackYears = 10`. Erforderlich sind mindestens `MinimumSeasonalSamples = 2` Beobachtungen. Aktuelle und zukünftige Monate werden auch dann ausgeschlossen, wenn sie in der Datei stehen. Ohne ausreichende Historie keine neuen Positionen. Tatsächliche Monatskurse des Instruments sind bereitzustellen; es gibt keine feste Monatstabelle.
