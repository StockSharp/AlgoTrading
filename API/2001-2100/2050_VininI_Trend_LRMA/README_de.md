# VininI Trend LRMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verfolgt den Endpunkt `L` einer rollierenden linearen Regression abgeschlossener Schlusskurse. Standard: `CandleType = 4 Stunden`, `Period = 13`, `UpLevel = 10`, `DnLevel = -10`, `Mode = Breakdown`. Handel erst mit gebildeten Indikatoren und Erlaubnis.

In **Breakdown** gelten Schwellen für den relativen Oszillator `O = 100 × (Close − L) / L`, in Prozent. Kauf bei `previousO <= UpLevel && O > UpLevel`, Verkauf bei `previousO >= DnLevel && O < DnLevel`. Es sind keine absoluten LRMA-Kursniveaus.

In **Twist** wird gekauft, wenn die Endpunktsteigung nach negativer Richtung positiv wird; verkauft beim umgekehrten Wechsel. Eine flache Strecke erhält die vorherige Richtung ungleich null.

Kauf bei `Position <= 0`, Verkauf bei `Position >= 0`. Erst ausgeführte Gegenposition schließen, dann `Volume` in neuer Richtung über Marktaufträge eröffnen. Gleichgerichtete Signale vergrößern Positionen nicht. Kein Stop-Loss oder Take-Profit.
