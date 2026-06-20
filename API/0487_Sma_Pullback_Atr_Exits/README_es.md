# Estrategia de Pullback SMA + Salidas ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en pullbacks cuando una media móvil de corto plazo está por encima o por debajo de una media de tendencia de largo plazo. Las posiciones largas se abren cuando el precio cae por debajo de la SMA rápida mientras permanece por encima de la SMA lenta. Las posiciones cortas se abren cuando el precio sube por encima de la SMA rápida mientras permanece por debajo de la SMA lenta. Las salidas utilizan múltiplos del Average True Range desde el precio de entrada.

## Detalles

- **Criterios de entrada**:
  - Largo: close < SMA rápida y SMA rápida > SMA lenta.
  - Corto: close > SMA rápida y SMA rápida < SMA lenta.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - El precio alcanza el stop loss o take profit basado en ATR.
- **Stops**: Múltiplos de ATR para stop loss y take profit.
- **Valores predeterminados**:
  - `FastSmaLength` = 8
  - `SlowSmaLength` = 30
  - `AtrLength` = 14
  - `AtrMultiplierSl` = 1.2
  - `AtrMultiplierTp` = 2.0
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, ATR
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
