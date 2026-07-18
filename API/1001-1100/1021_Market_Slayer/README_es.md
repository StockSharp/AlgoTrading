# Estrategia Market Slayer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza un cruce de medias móviles ponderadas con confirmación de tendencia SSL en un marco temporal superior. Se abre una posición larga cuando la WMA corta cruza por encima de la WMA larga con tendencia alcista; se abre una posición corta en condiciones opuestas. Se puede habilitar opcionalmente un take profit y stop loss absolutos.

## Detalles

- **Criterios de entrada**:
  - **Largo**: la WMA corta cruza por encima de la WMA larga y el SSL del marco temporal superior es alcista.
  - **Corto**: la WMA corta cruza por debajo de la WMA larga y el SSL del marco temporal superior es bajista.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - El filtro de tendencia cambia al lado opuesto.
  - Stop loss o take profit opcionales cuando están habilitados.
- **Stops**: Opcional.
- **Valores predeterminados**:
  - `ShortLength` = 10.
  - `LongLength` = 20.
  - `ConfirmationTrendValue` = 2.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
  - `TrendCandleType` = TimeSpan.FromMinutes(240).TimeFrame().
  - `TakeProfitEnabled` = false.
  - `TakeProfitValue` = 20.
  - `StopLossEnabled` = false.
  - `StopLossValue` = 50.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: WMA, SSL
  - Stops: Opcional
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
