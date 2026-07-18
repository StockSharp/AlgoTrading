# Feinabstimmung der Eingaben: Fourier-geglättete hybride Volumen-Spread-Analyse
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert geglättetes Volumen mit dem EMA der Eröffnungs- und Schlusskurse, um den Volumen-Spread zu analysieren. Sie geht long, wenn sowohl der Volumen-Spread als auch sein gleitender Durchschnitt positiv sind, und short, wenn beide negativ sind. Ein optionaler Parameter erlaubt das Schließen von Positionen bei fehlendem Signal.

## Details

- **Einstiegsbedingungen**:
  - **Long**: `vd > 0` und `vdma > 0`
  - **Short**: `vd < 0` und `vdma < 0`
- **Ausstiegsbedingungen**: Optional Position schließen, wenn Signale neutral sind.
- **Typ**: Trendfolge
- **Indikatoren**: EMA
- **Zeitrahmen**: 1 Minute (Standard)
