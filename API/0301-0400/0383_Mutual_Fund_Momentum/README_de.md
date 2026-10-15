# Investmentfonds-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie rotiert vierteljährlich zwischen einer Reihe von Investmentfonds. Am Ende jedes Quartals werden die Fonds nach ihrer Sechs-Monats-Performance gerankt. Das Kapital wird für das nächste Quartal dem führenden Fonds zugewiesen, sodass Langzeitinvestoren dem anhaltenden Momentum in aktiv verwalteten Produkten folgen können.

Es wird jeweils nur ein Fonds gehalten. Tagespreisdaten werden verwendet und die Neugewichtung erfolgt in den ersten drei Handelstagen im Januar, April, Juli und Oktober.

## Details

- **Universum**: Liste von Investmentfonds.
- **Signal**: 126-Tage (Sechs-Monats)-Gesamtrendite-Ranking.
- **Neugewichtung**: vierteljährlich an den ersten Handelstagen des neuen Quartals.
- **Positionierung**: vollständig Long im am höchsten eingestuften Fonds.
- **Risikokontrolle**: Handel überspringen, wenn der Auftragswert unter `MinTradeUsd` liegt.
