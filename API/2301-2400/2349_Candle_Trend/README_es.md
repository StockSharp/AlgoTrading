# Estrategia de Tendencia de Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia abre posiciones basándose en la dirección de velas consecutivas.
Se abre una posición larga después de que aparezca un número especificado de velas alcistas seguidas, mientras que se abre una posición corta después del mismo número de velas bajistas.
Las posiciones existentes pueden cerrarse cuando ocurra la señal opuesta.

## Parámetros

- **Candle Type**: Marco temporal de las velas utilizadas para el análisis.
- **Trend Candles**: Número de velas consecutivas en una dirección necesarias para activar una acción.
- **Take Profit %**: Take-profit opcional expresado como porcentaje del precio de entrada.
- **Stop Loss %**: Stop-loss opcional expresado como porcentaje del precio de entrada.
- **Enable Long Entry**: Permitir abrir posiciones largas.
- **Enable Short Entry**: Permitir abrir posiciones cortas.
- **Enable Long Exit**: Permitir cerrar posiciones largas en señal opuesta.
- **Enable Short Exit**: Permitir cerrar posiciones cortas en señal opuesta.

## Lógica

1. Suscribirse a los datos de velas del marco temporal seleccionado.
2. Seguir el número de velas alcistas y bajistas consecutivas.
3. Cuando el contador alcista alcanza el número requerido:
   - Cerrar posiciones cortas si está permitido.
   - Abrir una posición larga si está permitido.
4. Cuando el contador bajista alcanza el número requerido:
   - Cerrar posiciones largas si está permitido.
   - Abrir una posición corta si está permitido.
5. Las órdenes de protección opcionales se establecen usando `StartProtection`.

## Notas

- Las señales se procesan solo en velas completadas.
- La estrategia usa `BuyMarket` y `SellMarket` para entradas y salidas.
- Todos los comentarios en el código están escritos en inglés según lo requerido.
