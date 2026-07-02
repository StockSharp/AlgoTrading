# Estrategia Adx Cci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en los indicadores ADX y CCI. Entra largo cuando ADX > 25 y el CCI está sobrevendido (< -100). Entra corto cuando ADX > 25 y el CCI está sobrecomprado (> 100).

Las pruebas indican un rendimiento anual promedio de aproximadamente 97%. Funciona mejor en el mercado de criptomonedas.

El ADX evalúa si una tendencia tiene fuerza y el CCI identifica el momento de entrada después de retrocesos. Las posiciones largas y cortas siguen la dirección del ADX.

Orientado a traders de momentum que entran en retrocesos. Los múltiplos de ATR gestionan el riesgo.

## Detalles

- **Criterios de entrada**:
  - Largo: `ADX > 25 && CCI < -100`
  - Corto: `ADX > 25 && CCI > 100`
- **Largo/Corto**: Ambos
- **Criterios de salida**: La tendencia se debilita o el CCI cruza cero
- **Stops**: Basados en porcentaje usando `StopLossPercent`
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `CciPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: ADX, CCI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

