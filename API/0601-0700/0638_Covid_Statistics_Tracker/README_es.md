# Estrategia de Seguimiento de Estadísticas Covid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera en base a la tasa de crecimiento de los casos confirmados de COVID-19.
La estrategia vende cuando el crecimiento de casos se acelera y compra cuando el crecimiento se desacelera.

## Detalles

- **Criterios de entrada**:
  - Largo: `growth < 1`
  - Corto: `growth > 1`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `Region` = "US"
  - `Lookback` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Otro
  - Dirección: Ambos
  - Indicadores: Personalizado
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
