# Estrategia Night Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Night Stochastic opera únicamente durante la tranquila sesión nocturna de **21:00** a **06:00**. Utiliza la línea %K del Stochastic Oscillator para detectar condiciones de sobreventa y sobrecompra.

Cuando el oscilador cae por debajo del nivel de sobreventa se abre una posición larga. Cuando sube por encima del nivel de sobrecompra se abre una posición corta. Cada operación está protegida por niveles fijos de stop loss y take profit medidos en puntos de precio.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `%K < StochOversold` y el tiempo está entre 21:00 y 06:00.
  - **Corto**: `%K > StochOverbought` y el tiempo está entre 21:00 y 06:00.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Posición cerrada por stop loss o take profit predefinidos.
- **Stops**: Sí, utiliza stop loss y take profit fijos.
- **Valores predeterminados**:
  - `StopLossPoints` = 40
  - `TakeProfitPoints` = 20
  - `StochOversold` = 30
  - `StochOverbought` = 70
  - `CandleType` = marco temporal de 15 minutos
- **Filtros**:
  - Categoría: Basado en indicadores
  - Dirección: Ambos
  - Indicadores: Stochastic Oscillator
  - Marco temporal: Corto plazo
  - Ventana de trading: 21:00-06:00 hora del servidor
