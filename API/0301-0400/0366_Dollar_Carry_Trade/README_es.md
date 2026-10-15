# Carry Trade del Dólar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de **Carry Trade del Dólar** clasifica los pares de divisas en USD por diferencial de tasas de interés y va largo en USD frente a divisas de bajo carry y corto frente a divisas de alto carry. Rebalancea mensualmente el primer día de negociación.

## Detalles
- **Criterios de entrada**: Clasificar por carry; largo en bajo carry, corto en alto carry.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Rebalanceo mensual.
- **Stops**: Sin stop explícito.
- **Valores predeterminados**:
  - `K = 3`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Fundamental
  - Dirección: Ambos
  - Indicadores: Rates
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
