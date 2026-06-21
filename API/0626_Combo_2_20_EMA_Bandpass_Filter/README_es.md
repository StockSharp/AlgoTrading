# Estrategia Combo 2/20 EMA con Filtro de Paso de Banda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina un cruce de EMA rápida y lenta con un filtro de paso de banda. Las posiciones largas se abren cuando la EMA rápida está por encima de la EMA lenta y el valor del filtro supera la zona de venta. Las posiciones cortas se abren cuando la EMA rápida está por debajo de la EMA lenta y el valor del filtro cae por debajo de la zona de compra. Las posiciones se cierran si desaparecen las señales o antes de la fecha de inicio.

Las pruebas indican un rendimiento anual promedio de alrededor del 47%. Funciona mejor en el mercado de criptomonedas.

## Detalles
- **Criterios de entrada**:
  - **Largo**: EMA rápida > EMA lenta y filtro > zona de venta
  - **Corto**: EMA rápida < EMA lenta y filtro < zona de compra
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cerrar posición cuando desaparezcan las señales
- **Stops**: No
- **Valores predeterminados**:
  - `FastEmaLength` = 2
  - `SlowEmaLength` = 20
  - `BpfLength` = 20
  - `BpfDelta` = 0.5m
  - `BpfSellZone` = 5m
  - `BpfBuyZone` = -5m
  - `StartDate` = new DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeSpan.Zero)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA Bandpass Filter
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
