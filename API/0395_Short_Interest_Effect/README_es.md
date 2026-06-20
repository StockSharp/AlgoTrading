# Estrategia de Efecto del Interés en Corto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Efecto del Interés en Corto** utiliza los niveles de interés en corto para predecir el rendimiento de las acciones. Los valores con pocos días para cubrir tienden a superar a los que tienen mucha posición corta. En un intervalo mensual, las acciones se ordenan por interés en corto y la cartera compra el grupo con menor nivel mientras vende en corto el de mayor nivel.

## Detalles
- **Criterios de entrada**: Clasificación mensual por ratio de interés en corto o días para cubrir.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Rebalanceo mensual.
- **Stops**: Sin stop explícito.
- **Valores predeterminados**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Fundamental
  - Dirección: Ambos
  - Indicadores: Fundamentales
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
