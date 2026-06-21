# Estrategia de Formas del Cuerpo de las Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera en función de dónde abre y cierra una vela dentro de su rango.
Entra en largo cuando la vela abre cerca de su mínimo y cierra cerca de su máximo, mostrando una fuerte presión alcista.
Entra en corto cuando la vela abre cerca de su máximo y cierra cerca de su mínimo, indicando una fuerte presión bajista.

El enfoque se basa puramente en la acción del precio y puede aplicarse a cualquier mercado líquido.

## Detalles

- **Criterios de entrada**:
  - Largo: `Open near Low && Close near High`
  - Corto: `Open near High && Close near Low`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `BodyThreshold` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Patrón de velas japonesas
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
