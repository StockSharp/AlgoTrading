# Larry Connors 3-Tage-Hoch-Tief-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert den Larry Connors 3-Tage-Hoch/Tief-Mean-Reversion-Ansatz.

## Logik

- Kaufen, wenn:
  - Der Schlusskurs über dem langen gleitenden Durchschnitt liegt.
  - Der Schlusskurs unter dem kurzen gleitenden Durchschnitt liegt.
  - Hoch und Tief drei aufeinanderfolgende Kerzen niedriger waren.
- Ausstieg, wenn der Preis über dem kurzen gleitenden Durchschnitt schließt.

## Parameter

- **Long MA Length** — Periode für den langen SMA (Standard 200)
- **Short MA Length** — Periode für den kurzen SMA (Standard 5)
- **Candle Type** — Zeitrahmen für die Analyse
