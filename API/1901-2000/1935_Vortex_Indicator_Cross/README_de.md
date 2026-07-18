# Strategie mit Vortex-Indikator-Kreuzung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt die Kreuzungen der positiven (VI+) und negativen (VI-) Linien des Vortex-Indikators.
Wenn VI+ über VI- kreuzt, geht die Strategie long; wenn VI- über VI+ kreuzt, geht sie short.
Stop-Loss und Take-Profit in Preisschritten werden automatisch verwaltet.

## Parameter

- **Vortex Length** – Periode des Vortex-Indikators.
- **Candle Type** – Zeitrahmen für die Indikatorberechnung.
- **Stop Loss** – Schutz-Stop in Preisschritten.
- **Take Profit** – Zielgewinn in Preisschritten.

## Details

- **Indikatoren**: Vortex
- **Richtung**: Long und Short
- **Zeitrahmen**: Konfigurierbar
- **Risikomanagement**: Stop-Loss und Take-Profit über `StartProtection`.
