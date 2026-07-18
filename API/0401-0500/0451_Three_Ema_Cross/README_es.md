# Estrategia de Cruce de Tres EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Three EMA Cross combina un clásico cruce de media móvil rápida/lenta con
un filtro de tendencia más largo. Después de que la EMA rápida cruza por encima de la
EMA lenta, la estrategia espera un retroceso hacia la media rápida mientras el precio
de cierre permanece por encima de una EMA de tendencia más amplia. Esta configuración
intenta capturar movimientos de continuación después de una breve corrección dentro de
la tendencia prevalente.

Las posiciones se cierran cuando el momentum se desvanece y la EMA rápida cruza de
nuevo por debajo de la EMA lenta. Un stop loss basado en porcentaje protege la posición
si el precio se mueve en contra de la operación. La técnica funciona bien en mercados
con tendencias persistentes y tiende a evitar rangos laterales.

## Detalles

- **Criterios de entrada**:
  - Cruce reciente de EMA rápida por encima de EMA lenta dentro de los últimos *N* barras.
  - Cierre actual ≥ EMA rápida y mínimo de sesión ≤ EMA rápida.
  - EMA de tendencia ≤ cierre actual.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - EMA rápida cae por debajo de EMA lenta.
- **Stops**: Stop loss en `stop_loss_percent` del precio de entrada.
- **Valores predeterminados**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 20
  - `TrendEmaLength` = 100
  - `StopLossPercent` = 2.0
  - `CrossBackBars` = 10
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
