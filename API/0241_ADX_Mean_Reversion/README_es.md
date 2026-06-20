# Estrategia de Reversión a la Media con ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Aquí el Average Directional Index (ADX) mide la fuerza general de la tendencia. Cuando el ADX es bajo, el mercado carece de dirección y los precios tienden a oscilar alrededor de un valor medio. Esta estrategia explota ese comportamiento operando las desviaciones del ADX de su media móvil.

Las pruebas indican un retorno anual promedio de aproximadamente 70%. Funciona mejor en el mercado de acciones.

Se entra en una operación larga cuando el ADX cae por debajo del promedio menos `DeviationMultiplier` veces la desviación estándar y el precio está por debajo de la media móvil. Se abre una operación corta cuando el ADX sube por encima de la banda superior y el precio está por encima del promedio. Las posiciones se cierran cuando el ADX revierte hacia su promedio.

Este sistema atrae a traders que buscan oportunidades durante entornos de baja tendencia. El stop-loss evita que las pequeñas operaciones de reversión a la media se conviertan en grandes pérdidas si surge una nueva tendencia.

## Detalles
- **Criterios de entrada**:
  - **Largo**: ADX < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Corto**: ADX > Avg + DeviationMultiplier * StdDev && Close > MA
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando ADX > Avg
  - **Corto**: Salir cuando ADX < Avg
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mean reversion
  - Dirección: Ambos
  - Indicadores: ADX
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

