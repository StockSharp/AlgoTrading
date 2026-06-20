# Estrategia del Efecto de Crecimiento de Activos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia toma posiciones largas en empresas con el menor crecimiento de activos totales y cortas en aquellas con el mayor crecimiento de activos. Cada julio el portafolio se rebalancea utilizando los datos fundamentales más recientes.

Las pruebas indican un rendimiento anual promedio de aproximadamente 15%. Funciona mejor en el mercado de renta variable.

El crecimiento de activos se calcula a partir de los activos totales declarados en los informes de las empresas. Las acciones se clasifican en cuantiles y el cuantil inferior se compra mientras que el superior se vende en corto. Las posiciones se dimensionan para alcanzar un apalancamiento objetivo y se ajustan anualmente.

## Detalles

- **Criterios de entrada**:
  - Largo: Acción en el cuantil de menor crecimiento de activos.
  - Corto: Acción en el cuantil de mayor crecimiento de activos.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Posiciones ajustadas en el rebalanceo anual.
- **Stops**: No.
- **Valores predeterminados**:
  - `Quantiles` = 10
  - `Leverage` = 1m
  - `MinTradeUsd` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Fundamental
  - Dirección: Ambos
  - Indicadores: Fundamentals
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Largo plazo
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
