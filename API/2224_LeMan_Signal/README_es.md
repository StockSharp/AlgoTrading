# Estrategia de Señal LeMan
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La Estrategia de Señal LeMan es un port del asesor experto LeManSignal original de MetaTrader. El enfoque analiza los máximos y mínimos recientes en dos períodos secuenciales para detectar posibles reversiones de tendencia. Cuando se encuentran patrones específicos, se abre una posición larga o corta en la siguiente vela.

## Cómo funciona

1. La estrategia observa velas completas del marco temporal seleccionado.
2. Para la barra anterior compara los máximos más altos y los mínimos más bajos en dos rangos consecutivos:
   - `H1` y `H2` son los máximos de dos rangos adyacentes.
   - `H3` y `H4` son los máximos del siguiente par de rangos.
   - `L1` y `L2` son los mínimos de dos rangos adyacentes.
   - `L3` y `L4` son los mínimos del siguiente par de rangos.
3. Se activa una señal de **compra** si `H3 <= H4` y `H1 > H2`.
4. Se activa una señal de **venta** si `L3 >= L4` y `L1 < L2`.
5. Las órdenes se ejecutan a precio de mercado. Cualquier posición contraria abierta se cierra automáticamente.
6. La gestión de riesgo opcional se aplica mediante `StartProtection` con valores predeterminados de stop-loss y take-profit del 1% y 2% respectivamente.

## Parámetros

- **Period** – longitud del período de lookback del indicador.
- **Signal Bar** – desplazamiento utilizado para confirmar la señal (predeterminado 1).
- **Candle Type** – marco temporal de las velas a analizar.

## Notas

- La estrategia solo reacciona a las velas terminadas.
- No mantiene colecciones adicionales; los búferes internos se limitan al mínimo necesario para los cálculos.
- Para usar la estrategia, añádala a un terminal StockSharp, establezca el instrumento y los parámetros deseados, y comience la estrategia.
