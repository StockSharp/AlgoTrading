# Estrategia de Momentum y Crecimiento de Activos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de factores híbrida combina el momentum de precios con el efecto de crecimiento de activos. Las empresas que expanden rápidamente sus balances y simultáneamente muestran precios con tendencias sólidas suelen ser recompensadas por el mercado. El enfoque primero filtra el universo para seleccionar las empresas en el decil superior de crecimiento de activos.

Las acciones elegibles se clasifican entonces por momentum de doce meses, excluyendo el mes más reciente para evitar reversiones a corto plazo. Se compra el quintil superior por momentum mientras se vende en corto el quintil inferior. El rebalanceo tiene lugar en el primer día hábil de cada mes, excepto enero, cuando la estrategia permanece inactiva. No se aplican stops entre revisiones.

Las pruebas retrospectivas en renta variable desarrollada indican que la combinación de expansión de activos y momentum ofrece rendimientos robustos con una rotación moderada.

## Detalles

- **Criterios de entrada**: Mensual; seleccionar el decil superior de crecimiento de activos y luego clasificar por
  momentum; largo quintil superior, corto quintil inferior
- **Largo/Corto**: Ambos
- **Criterios de salida**: Próximo rebalanceo mensual (enero omitido)
- **Stops**: No
- **Valores predeterminados**:
  - `MomLook` = 252
  - `SkipMonths` = 1
  - `AssetDecile` = 10
  - `Quintile` = 5
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Momentum, Fundamentales
  - Dirección: Ambos
  - Indicadores: Momentum de precio, crecimiento de activos
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Medio plazo
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
