# Bar-Balance-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie misst das Gleichgewicht zwischen Aufwärts- und Abwärtsbewegungen innerhalb jeder Kerze. Ein positives Gleichgewicht deutet darauf hin, dass Käufer die Bar dominieren, während ein negatives Gleichgewicht auf Verkaufsdruck hinweist.

Das System glättet dieses Gleichgewicht mit einem gleitenden Durchschnitt. Wenn sowohl das aktuelle Gleichgewicht als auch sein Durchschnitt über null liegen, geht die Strategie eine Long-Position ein. Wenn beide unter null fallen, geht sie Short.

## Details

- **Einstiegskriterien**: Gleichgewicht > 0 und Durchschnitt > 0 für Long; Gleichgewicht < 0 und Durchschnitt < 0 für Short.
- **Ausstiegskriterien**: Das entgegengesetzte Signal löst eine Positionsumkehr aus.
- **Indikatoren**: benutzerdefiniertes Bar Balance, SMA.
- **Long/Short**: beide.
- **Stop-Loss**: keine.
