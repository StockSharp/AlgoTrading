# Estrategia del Indicador Stalin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica la lógica del indicador "Stalin" de MQL5.
Usa un par de medias móviles exponenciales (EMAs) y un filtro RSI opcional.
Una señal larga aparece cuando la EMA rápida cruza por encima de la EMA lenta y el RSI está por encima de 50.
Una señal corta aparece cuando la EMA rápida cruza por debajo de la EMA lenta y el RSI está por debajo de 50.

Las señales pueden confirmarse mediante un movimiento de precio requerido y filtrarse por la distancia desde la última señal.
Las posiciones se abren con órdenes de mercado y se invierten ante señales opuestas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `FastEMA(t-1) < SlowEMA(t-1)` && `FastEMA(t) > SlowEMA(t)` && `RSI(t) > 50`.
  - **Corto**: `FastEMA(t-1) > SlowEMA(t-1)` && `FastEMA(t) < SlowEMA(t)` && `RSI(t) < 50`.
- **Confirmar**: La operación se demora hasta que el precio se mueve `Confirm` puntos desde el nivel de ruptura.
- **Filtro Flat**: Las nuevas señales se ignoran si están más cerca que `Flat` puntos del precio de la señal anterior.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `FastLength` = 14.
  - `SlowLength` = 21.
  - `RsiLength` = 17.
  - `Confirm` = 0 puntos (deshabilitado).
  - `Flat` = 0 puntos (deshabilitado).
  - `CandleType` = velas de 1 hora.
- **Filtros**:
  - Categoría: Seguimiento de tendencia.
  - Dirección: Ambos.
  - Indicadores: Múltiples.
  - Stops: No.
  - Complejidad: Moderado.
  - Marco temporal: Medio plazo.
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Medio.
