# Estrategia Modular de Operaciones en Rango
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia está orientada a mercados en rango mediante dos módulos que no pueden estar activos al mismo tiempo. El primer módulo se basa en la confirmación de momentum con MACD junto con RSI y reversión a la media de las Bandas de Bollinger. El segundo módulo compra o vende en extremos cuando el precio rebota de vuelta dentro de las Bandas de Bollinger con niveles de RSI sobrevendido o sobrecomprado. Los stops basados en ATR y salidas opcionales a través de Bandas de Bollinger o reversiones del RSI gestionan el riesgo.

## Detalles

- **Criterios de entrada**:
  - **Lógica 1 Largo**: ADX por debajo del umbral, MACD cruza por encima de la señal, RSI por encima de su SMA, precio por debajo de la banda media de Bollinger.
  - **Lógica 1 Corto**: ADX por debajo del umbral, MACD cruza por debajo de la señal, RSI por debajo de su SMA, precio por encima de la banda media de Bollinger.
  - **Lógica 2 Largo**: ADX por debajo del umbral, precio cruza de vuelta por encima de la banda inferior, RSI por debajo del nivel de sobreventa.
  - **Lógica 2 Corto**: ADX por debajo del umbral, precio cruza de vuelta por debajo de la banda superior, RSI por encima del nivel de sobrecompra.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Stop loss por ATR.
  - Señales opcionales de Bollinger o RSI según la lógica activa.
- **Stops**: Múltiplos de ATR.
- **Valores predeterminados**: Bollinger 20/2, RSI 14, MACD 12/26/9, ATR 14, ADX 14.
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Complejo
  - Marco temporal: Medio plazo
