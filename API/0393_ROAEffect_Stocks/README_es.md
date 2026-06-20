# Estrategia de Efecto ROA en Acciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Efecto ROA en Acciones** se enfoca en valores con alto retorno sobre activos (ROA). Un feed externo de datos fundamentales proporciona los valores ROA para el universo de trading. Al inicio de cada mes, las acciones se clasifican por ROA, y la cartera toma posiciones largas en el decil superior y cortas en el decil inferior.

Las posiciones tienen igual ponderación y se rebalancean mensualmente, capturando la tendencia de las empresas rentables a superar al mercado.

## Detalles
- **Criterios de entrada**: Clasificación mensual por datos externos de ROA.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Rebalanceo mensual.
- **Stops**: Sin stop explícito.
- **Valores predeterminados**:
  - `Decile = 10`
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Fundamental
  - Dirección: Ambos
  - Indicadores: Fundamentales
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
