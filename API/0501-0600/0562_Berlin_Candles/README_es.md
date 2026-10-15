# Estrategia Berlin Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza velas Berlin personalizadas derivadas de valores Heikin Ashi suavizados. Se abre una posición larga cuando una vela Berlin alcista cierra por encima de la línea base de Donchian. Se abre una posición corta cuando una vela Berlin bajista cierra por debajo de la línea base.

## Detalles

- **Criterios de entrada**:
  - **Largo**: cierre Berlin > apertura Berlin y cierre Berlin > línea base.
  - **Corto**: cierre Berlin < apertura Berlin y cierre Berlin < línea base.
- **Largo/Corto**: Ambos
- **Stops**: Ninguno por defecto
- **Valores predeterminados**:
  - `Smoothing` = 1
  - `BaselinePeriod` = 26
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
