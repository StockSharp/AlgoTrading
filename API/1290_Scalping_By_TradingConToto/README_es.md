# Estrategia de Scalping de TradingConToto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Scalping de TradingConToto traza líneas entre máximos o mínimos de pivote consecutivos según la tendencia de la EMA. Cuando el precio cruza por encima de una línea descendente de máximos de pivote durante una tendencia alcista, la estrategia entra largo. Cuando el precio cae por debajo de una línea ascendente de mínimos de pivote durante una tendencia bajista, entra corto. La operativa solo está permitida durante una sesión especificada.

## Detalles

- **Criterios de entrada**: Tendencia alcista con el precio rompiendo una línea descendente de máximos de pivote para largo; tendencia bajista con el precio rompiendo una línea ascendente de mínimos de pivote para corto.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Take profit y stop loss.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Pivot` = 16
  - `Pips` = 64
  - `Spread` = 0
  - `Session` = "0830-0930"
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: EMA, pivot
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
