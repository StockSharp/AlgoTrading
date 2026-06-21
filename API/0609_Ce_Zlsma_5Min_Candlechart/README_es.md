# Estrategia CE ZLSMA 5MIN Candlechart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de seguimiento de tendencia que usa ZLSMA en velas Heikin Ashi con un filtro Chandelier Exit. Compra cuando la tendencia gira alcista y la vela cierra por encima del ZLSMA.

## Detalles

- **Criterios de entrada**:
  - Largo: la dirección gira hacia arriba, cierre de Heikin Ashi por encima de ZLSMA y apertura
- **Largo/Corto**: Largo
- **Criterios de salida**:
  - Largo: cierre por debajo de ZLSMA
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `ZlsmaLength` = 50
  - `AtrPeriod` = 1
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo
  - Indicadores: ZLSMA, ATR, Heikin Ashi
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
