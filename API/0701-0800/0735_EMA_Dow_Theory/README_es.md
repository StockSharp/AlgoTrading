# Estrategia EMA con Teoría de Dow
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el cruce de una Media Móvil Exponencial (EMA) rápida y lenta con un filtro de tendencia básico de la Teoría de Dow. La tendencia se determina por los máximos y mínimos de oscilación recientes. Se toman posiciones cuando las EMA se alinean con la dirección de la tendencia.

## Detalles

- **Criterios de entrada**:
  - **Largo**: EMA rápida ≥ EMA lenta y el precio rompe por encima del último máximo de oscilación.
  - **Corto**: EMA rápida < EMA lenta y el precio rompe por debajo del último mínimo de oscilación.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - Longitud EMA rápida = 47
  - Longitud EMA lenta = 50
  - Longitud de oscilación = 6 barras
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, máximo/mínimo de oscilación
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
