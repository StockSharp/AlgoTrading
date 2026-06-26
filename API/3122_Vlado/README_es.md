# Estrategia de Vlado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de reversión de momentum basada en el clásico oscilador Williams %R de Larry Williams. El sistema espera a que el oscilador alcance lecturas extremas de sobreventa o sobrecompra y luego revierte la posición en la siguiente barra completada. El port de StockSharp mantiene el carácter discrecional de la implementación original de MetaTrader, exponiendo cada ajuste importante como un parámetro.

## Visión general

- **Categoría**: Estrategia de oscilador de reversión a la media.
- **Mercado**: Cualquier instrumento líquido que ofrezca datos de velas estables (pares de forex, futuros de índices, pares spot de criptomonedas).
- **Marco temporal**: Configurable mediante `CandleType`. Por defecto velas de 1 hora, coincidiendo con el ejemplo de uso original.
- **Dirección**: Largo y corto. El motor siempre mantiene como máximo una posición y gira cuando aparece la señal opuesta.
- **Indicador**: Williams %R con longitud de lookback y niveles de umbral configurables.

## Cómo Funciona

1. Se suscribe al feed de velas seleccionado y calcula Williams %R en cada vela terminada.
2. Usa el nivel de sobreventa predeterminado de -75 y el nivel de sobrecompra de -25 (los valores son negativos debido a la escala del oscilador).
3. Cuando %R cae por debajo del nivel de sobreventa, la estrategia entra o revierte a una posición larga.
4. Cuando %R sube por encima del nivel de sobrecompra, la estrategia entra o revierte a una posición corta.
5. Las órdenes se dimensionan con `Volume + Math.Abs(Position)` de modo que una reversión cierra la posición existente y abre la nueva en una única orden de mercado.
6. No se utiliza stop-loss ni take-profit explícito. El riesgo se controla mediante los niveles del indicador y el marco temporal elegido.
7. Cada acción se registra mediante `LogInfo`, lo que facilita auditar las operaciones en la GUI de StockSharp o en los archivos de log.

## Parámetros

- `WilliamsPeriod`: Número de velas utilizadas para calcular el oscilador. Valores más altos suavizan la señal, valores más bajos reaccionan más rápido.
- `OverboughtLevel`: Umbral que define cuándo se considera que el mercado está sobrecomprado (por defecto -25). Se puede optimizar.
- `OversoldLevel`: Umbral que define cuándo se considera que el mercado está sobrevendido (por defecto -75). Se puede optimizar.
- `CandleType`: Tipo de vela y marco temporal aplicado a todos los cálculos. Funciona con marcos de tiempo, velas de volumen o barras de rango.
- `Volume` (heredado de `Strategy`): Define el tamaño base de la orden. Ajustar según el tamaño de la cuenta y el apetito de riesgo.

## Reglas de Trading

- **Entrada larga**: Se activa cuando `%R <= OversoldLevel` y la posición actual es plana o corta.
- **Entrada corta**: Se activa cuando `%R >= OverboughtLevel` y la posición actual es plana o larga.
- **Salida**: Realizada implícitamente por la orden de reversión cuando aparece una señal opuesta.
- **Gestión de posición**: Siempre una única posición abierta. El algoritmo no hace piramidación ni escala de salida.

## Notas Adicionales

- Funciona mejor en mercados laterales o de tendencia lenta donde los osciladores pueden oscilar entre extremos.
- Se recomienda combinar la estrategia con controles de riesgo externos (stops de patrimonio, filtros de sesión) para el trading en vivo.
- La implementación incluye renderizado de gráficos: el área principal muestra velas y operaciones, mientras que un panel secundario traza Williams %R.
- Diseñada para mayor investigación: cada parámetro admite optimización dentro de los optimizadores de StockSharp.
