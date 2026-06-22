# Fisher Org v1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Fisher Transform-Indikator, um Trendumkehrungen zu erfassen. Eine Long-Position wird eröffnet, wenn der Indikator ein lokales Minimum bildet, während eine Short-Position eröffnet wird, wenn ein lokales Maximum erscheint. Gegensignale schließen eine bestehende Position.

## Regeln
- **Long**: `Fisher[t-2] > Fisher[t-1]` und `Fisher[t-1] <= Fisher[t]`
- **Short**: `Fisher[t-2] < Fisher[t-1]` und `Fisher[t-1] >= Fisher[t]`

## Parameter
- `Fisher Length` – Periode des Fisher Transform (Standard 7)
- `Candle Type` – Zeitrahmen der für Berechnungen verwendeten Kerzen

## Indikatoren
- Fisher Transform
