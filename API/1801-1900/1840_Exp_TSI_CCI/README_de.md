# Exp TSI CCI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet den True Strength Index (TSI) auf Basis des Commodity Channel Index (CCI) und handelt auf Kreuzungen mit einer Signallinie.

## Logik
- CCI mit dem angegebenen Zeitraum berechnen.
- CCI-Werte mit kurzen und langen Glättungslängen in den True Strength Index einleiten.
- Den resultierenden TSI mit einem EMA glätten, um eine Signallinie zu erhalten.
- Long gehen, wenn TSI über die Signallinie kreuzt.
- Short gehen, wenn TSI unter die Signallinie kreuzt.

## Parameter
- `Candle Type` – Zeitrahmen der für die Analyse verwendeten Kerzen.
- `CCI Period` – Zeitraum für den Commodity Channel Index.
- `TSI Short Length` – kurze Glättungslänge des TSI.
- `TSI Long Length` – lange Glättungslänge des TSI.
- `Signal Length` – EMA-Länge für die TSI-Signallinie.

## Indikatoren
- Commodity Channel Index
- True Strength Index
- Exponential Moving Average

## Haftungsausschluss
Diese Strategie wird ausschließlich zu Bildungszwecken bereitgestellt und stellt keine Anlageberatung dar.
