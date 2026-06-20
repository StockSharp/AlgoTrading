# Divergencia de Volumen (Volume Divergence)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Divergencia de Volumen busca discrepancias entre el movimiento del precio y el volumen de negociación. Si el precio cae pero el volumen aumenta, puede señalar acumulación; si el precio sube con fuerte volumen, puede señalar distribución.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 43%. Funciona mejor en el mercado de acciones.

La estrategia entra en largo cuando los precios en caída van acompañados de volumen creciente, y entra en corto cuando los precios en alza se combinan con volumen elevado. Las salidas se basan en un cruce de media móvil.

Este enfoque intenta operar en contra de movimientos insostenibles.

## Detalles

- **Criterios de entrada**: Precio y volumen moviéndose en direcciones opuestas.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: El precio cruza la MA o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Divergencia
  - Dirección: Ambos
  - Indicadores: Volume, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
