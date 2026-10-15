# Estrategia de Ejecución Instantánea
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra inmediatamente en una única posición en la primera vela completada y la gestiona con reglas simples de beneficio y riesgo. La dirección de la posición es seleccionable mediante parámetros. Una vez que se abre una operación, el algoritmo rastrea el beneficio y la pérdida y puede seguir el precio para proteger las ganancias.

La lógica reproduce el comportamiento del script MQL original que permitía la ejecución instantánea de órdenes de mercado con valores opcionales de take profit, stop loss y trailing stop.

## Detalles

- **Criterios de entrada**: abre una posición de mercado en la primera vela finalizada tras el inicio. La dirección está definida por el parámetro `Direction`.
- **Largo/Corto**: Ambos lados soportados.
- **Criterios de salida**:
  - Take profit alcanzado.
  - Stop loss alcanzado.
  - Trailing stop activado y el precio alcanza el nivel de trailing.
- **Stops**: Están disponibles take profit, stop loss y trailing stop.
- **Valores predeterminados**:
  - `TakeProfit` = 70 unidades de precio.
  - `StopLoss` = 0 (deshabilitado).
  - `TrailingStart` = 5 unidades de precio.
  - `TrailingSize` = 5 unidades de precio.
- **Filtros**:
  - Categoría: Utilidad
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Simple
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
