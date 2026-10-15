# Estrategia de Momentum en Materias Primas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Commodity Momentum** toma posiciones largas en las materias primas con el momentum más fuerte a 12 meses (omitiendo el mes más reciente).
Las posiciones se rebalancean el primer día de negociación de cada mes.

Las pruebas indican un rendimiento anual promedio de aproximadamente 10%. Funciona mejor en mercados de materias primas diversificados.

Las posiciones se ajustan mensualmente; no se utilizan señales intradía.

## Detalles
- **Criterios de entrada**: Comprar las `TopN` principales materias primas por momentum a 12 meses excluyendo el último mes.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Rebalanceo en la próxima fecha programada.
- **Stops**: Sin lógica de stop explícita.
- **Valores predeterminados**:
  - `TopN = 5`
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Solo largos
  - Indicadores: Price
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
