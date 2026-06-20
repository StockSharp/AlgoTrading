# Gann Swing Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en la técnica de ruptura de swing de Gann

Las pruebas indican un retorno anual promedio de aproximadamente 82%. Funciona mejor en el mercado de acciones.

Gann Swing Breakout rastrea los máximos y mínimos de swing del análisis de Gann. Una ruptura más allá del último swing inicia una operación en esa dirección y permanece abierta hasta que se viola el swing opuesto.

El método está diseñado para operadores que ven los puntos de swing pasados como soporte y resistencia importantes. Al operar en la ruptura, intenta aprovechar la siguiente etapa de una tendencia.


## Detalles

- **Criterios de entrada**: Señales basadas en MA, Gann.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `SwingLookback` = 5
  - `MaPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: MA, Gann
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Neural Networks: No
  - Divergencia: No
  - Nivel de riesgo: Medio

