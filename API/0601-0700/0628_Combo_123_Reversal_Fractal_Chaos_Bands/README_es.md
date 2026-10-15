# Estrategia Combo 123 Reversal y Fractal Chaos Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina un patrón de reversión 123 con una ruptura de Fractal Chaos Bands.
Las operaciones largas ocurren cuando se forma una reversión 123 alcista y el precio cierra por encima de la banda fractal superior.
Las operaciones cortas ocurren cuando una reversión 123 bajista coincide con un cierre por debajo de la banda fractal inferior.

## Detalles

- **Criterios de entrada**:
  - Largo: Señal larga de Reversal123 y cierre por encima de la banda fractal superior.
  - Corto: Señal corta de Reversal123 y cierre por debajo de la banda fractal inferior.
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `Length` = 15
  - `KSmoothing` = 1
  - `DLength` = 3
  - `Level` = 50m
  - `Pattern` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Patrón y ruptura
  - Dirección: Ambos
  - Indicadores: Stochastic Oscillator, Fractal Chaos Bands
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
