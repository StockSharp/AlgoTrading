# Estrategia Karacatica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Karacatica es un enfoque de seguimiento de tendencia que combina la acción del precio con el Índice Direccional Promedio (ADX). Busca situaciones en las que el precio de cierre actual es mayor o menor que el precio de cierre hace un número especificado de velas y confirma el movimiento con la dominancia de la línea +DI o -DI.

## Indicadores
- **Average Directional Index (ADX)** – mide la fuerza de la tendencia y proporciona los componentes +DI y -DI.
- **Comparación de precios** – verifica si el último cierre está por encima o por debajo del cierre hace *Period* velas.

## Parámetros
- `Period` – número de velas utilizado tanto para el cálculo del ADX como para el lookback de la comparación de precios. El valor predeterminado es 70.
- `TakeProfitPercent` – take-profit expresado como porcentaje del precio de entrada. El valor predeterminado es 2%.
- `StopLossPercent` – stop-loss expresado como porcentaje del precio de entrada. El valor predeterminado es 1%.
- `CandleType` – marco temporal de las velas a las que suscribirse. El valor predeterminado es 1 hora.

## Lógica de trading
- **Entrada larga**: `Close > Close[Period]` y `+DI > -DI` sin señal larga existente. Cierra las posiciones cortas y abre una larga.
- **Entrada corta**: `Close < Close[Period]` y `-DI > +DI` sin señal corta existente. Cierra las posiciones largas y abre una corta.
- **Protección de posición**: `StartProtection` aplica tanto los porcentajes de take-profit como de stop-loss.

## Notas de uso
- Diseñada para la API de alto nivel de StockSharp; se suscribe a velas y vincula el indicador ADX.
- La estrategia cierra automáticamente las posiciones opuestas cuando aparece una nueva señal.
- No se proporciona implementación en Python por ahora.

## Aviso legal
Este ejemplo es solo para fines educativos y no garantiza ganancias. El trading conlleva un riesgo significativo. Siempre pruebe las estrategias exhaustivamente antes de implementarlas en mercados en vivo.
