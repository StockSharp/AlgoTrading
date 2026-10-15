# Estrategia de Tendencia RSI MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el Índice de Fuerza Relativa (RSI) con un filtro de tendencia de media móvil.
Se abre una posición larga cuando el RSI cae por debajo de un nivel de compra especificado mientras la media móvil rápida está por encima de la media móvil lenta.
Se abre una posición corta cuando el RSI sube por encima de un nivel de venta especificado mientras la media móvil rápida está por debajo de la media móvil lenta.

## Parámetros

- `RSI Period` – longitud del indicador RSI.
- `RSI Buy Level` – valor de RSI por debajo del cual se abre una posición larga.
- `RSI Sell Level` – valor de RSI por encima del cual se abre una posición corta.
- `Fast MA Period` – período de la media móvil rápida.
- `Slow MA Period` – período de la media móvil lenta.
- `Candle Type` – serie de velas utilizada para los cálculos.

## Lógica

1. Suscribirse a la serie de velas seleccionada.
2. Calcular RSI, MA rápida y MA lenta para cada vela finalizada.
3. Detectar tendencia alcista cuando la MA rápida está por encima de la MA lenta y tendencia bajista cuando está por debajo.
4. Entrar largo cuando RSI < nivel de compra y la tendencia es alcista, cerrando posiciones cortas si las hay.
5. Entrar corto cuando RSI > nivel de venta y la tendencia es bajista, cerrando posiciones largas si las hay.

## Notas

- La estrategia utiliza órdenes de mercado para las entradas.
- Las señales de operación se procesan solo en velas finalizadas.
- Los parámetros están disponibles para optimización en la interfaz de usuario.
