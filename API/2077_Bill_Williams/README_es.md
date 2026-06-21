# Bill Williams
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bill Williams combina el indicador Alligator con rupturas de fractales. Las mandíbulas, dientes y labios deben divergir antes de que una ruptura del fractal más reciente active una orden.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - Calcular máximos y mínimos fractales de las últimas 5 velas.
  - La distancia entre la mandíbula y los dientes debe superar `GatorDivSlowPoints`.
  - La distancia entre los labios y los dientes debe superar `GatorDivFastPoints`.
  - **Largo**: El precio cierra por encima del último fractal alcista al menos `FilterPoints` puntos y la vela es alcista.
  - **Corto**: El precio cierra por debajo del último fractal bajista al menos `FilterPoints` puntos y la vela es bajista.
- **Criterios de salida**:
  - Ruptura opuesta.
  - Stop trailing en el último fractal opuesto.
- **Stops**: Stop trailing basado en fractales.
- **Valores predeterminados**:
  - `FilterPoints` = 30
  - `GatorDivSlowPoints` = 250
  - `GatorDivFastPoints` = 150
  - `CandleType` = velas de 1 hora
