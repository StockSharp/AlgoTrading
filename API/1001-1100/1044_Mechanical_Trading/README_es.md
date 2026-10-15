# Estrategia de Trading Mecánico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia mecánica basada en tiempo que ejecuta una operación en una hora especificada cada día. La dirección de la posición puede configurarse para ir largo o corto. La operación se protege automáticamente con niveles de take profit y stop loss basados en porcentaje.

## Detalles

- **Criterios de entrada**:
  - **Largo**: en `TradeHour` cuando `Short Mode` está desactivado.
  - **Corto**: en `TradeHour` cuando `Short Mode` está activado.
- **Largo/Corto**: Ambos, dependiendo de `Short Mode`.
- **Criterios de salida**:
  - `Profit Target (%)` por encima/debajo de la entrada.
  - `Stop Loss (%)` por debajo/encima de la entrada.
- **Stops**: Stop loss y take profit.
- **Valores predeterminados**:
  - `Profit Target (%)` = 0.4.
  - `Stop Loss (%)` = 0.2.
  - `Trade Hour` = 16.
- **Filtros**:
  - Categoría: Tiempo
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
