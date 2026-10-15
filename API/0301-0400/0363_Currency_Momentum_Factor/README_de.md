# Währungs-Momentum-Faktor-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Faktorstrategie ordnet Währungen nach mittelfristigem Momentum und baut ein Long/Short-Portfolio auf. Währungen mit der stärksten Performance im Beobachtungszeitraum werden gekauft, während die schwächsten in gleichen Größen leerverkauft werden.

Das Momentum wird anhand von Tageskerzen ausgewertet und das Buch am ersten Handelstag jedes Monats neu gewichtet. Orders unterhalb eines Mindest-USD-Werts werden ignoriert, um Rauschen zu reduzieren.

## Details

- **Universum**: Liste von Währungspaaren oder ETFs.
- **Signal**: Long die `K` Währungen mit dem höchsten Momentum und Short die `K` schwächsten.
- **Lookback**: Rendite berechnet über `Lookback` Tageskerzen (Standard 252).
- **Rebalancing**: Monatlich.
- **Positionierung**: Long/Short, dollarneutral.
- **Parameter**:
  - `Universe` – handelbare Währungssymbole.
  - `Lookback` – Anzahl der Kerzen für Momentum.
  - `K` – Anzahl der Assets für Long und Short.
  - `MinTradeUsd` – Mindesthandelsgröße.
  - `CandleType` – Kerzen-Zeitrahmen (Standard: 1 Tag).
- **Hinweis**: Dem Beispiel fehlt die echte Momentum-Berechnung zu Demonstrationszwecken.
