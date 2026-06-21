# Estrategia MAM Crossover Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia construida comparando medias móviles simples de los precios de cierre y apertura de las velas.
Una señal larga ocurre cuando la SMA del cierre cruza por encima de la SMA de la apertura y la barra anterior confirmó una transición desde abajo. Una señal corta aparece con el patrón opuesto. Las posiciones opuestas se cierran al invertirse la señal. Un stop-loss y una toma de ganancias fijos opcionales protegen las operaciones.

## Detalles

- **Criterios de entrada**: Patrón de cruces de SMA(close) y SMA(open) en las últimas dos barras.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce opuesto o stops protectores.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MaPeriod` = 20
  - `StopLossTicks` = 40
  - `TakeProfitTicks` = 190
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: Fijo
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
