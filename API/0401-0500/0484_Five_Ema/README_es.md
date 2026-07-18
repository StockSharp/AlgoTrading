# Estrategia 5 EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia 5 EMA marca una vela que cierra completamente por debajo o por encima de la EMA de 5 períodos. Si el precio rompe el extremo de la vela de señal dentro de tres barras y fuera de la ventana de bloqueo, la estrategia entra en la dirección del rompimiento. Los objetivos se basan en una relación riesgo-recompensa definida por el usuario y las operaciones pueden cerrarse forzosamente a una hora específica.

## Detalles

- **Criterios de entrada**:
  - Cierre de vela y máximo por debajo de la EMA → marcar para largo; comprar cuando el precio cruce por encima del máximo de la señal en 3 barras.
  - Cierre de vela y mínimo por encima de la EMA → marcar para corto; vender cuando el precio cruce por debajo del mínimo de la señal en 3 barras.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Stop en el extremo opuesto de la vela de señal.
  - Objetivo en `TargetRR` × riesgo.
  - Salida opcional a hora personalizada (`ExitHour`, `ExitMinute`).
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EmaLength` = 5
  - `TargetRR` = 3.0
  - `ExitHour` = 15, `ExitMinute` = 30
  - `BlockStartHour` = 15, `BlockStartMinute` = 0
  - `BlockEndHour` = 15, `BlockEndMinute` = 30
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo/Corto
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: 5 minutos
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
