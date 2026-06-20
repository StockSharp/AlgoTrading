# Estrategia de Debilitamiento ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Índice Direccional Promedio mide la fuerza de la tendencia. Cuando el ADX comienza a declinar, a menudo señala que el movimiento actual está perdiendo momentum. Este sistema opera contra esa tendencia debilitante cuando el precio está al lado opuesto de una media móvil simple.

Las pruebas indican una rentabilidad anual media de aproximadamente el 136%. Funciona mejor en el mercado de acciones.

Para cada barra, la estrategia calcula el ADX y una MA. Si el ADX disminuye respecto al valor anterior y el precio está por encima de la MA, se coloca una entrada larga. Si el ADX cae mientras el precio está por debajo de la MA, se va corto. Un stop-loss fijo protege la posición.

Dado que el enfoque anticipa una desaceleración en lugar de una reversión completa, las operaciones generalmente se mantienen solo hasta que el ADX comienza a subir de nuevo o se alcanza el stop.

## Detalles

- **Criterios de entrada**: ADX inferior al valor anterior y precio relativo a la MA.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss.
- **Stops**: Sí, basado en porcentaje.
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `MaPeriod` = 20
  - `StopLoss` = 2%
  - `CandleType` = 15 minute
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: ADX, MA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

