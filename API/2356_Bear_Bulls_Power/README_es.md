# Estrategia Bear Bulls Power
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión del experto MetaTrader 5 "Exp_Bear_Bulls_Power". Utiliza un indicador Bear/Bulls Power suavizado para detectar reversiones de tendencia.

## Cómo funciona

1. Calcular el precio mediano de cada vela: `(High + Low) / 2`.
2. Suavizar el precio mediano con una media móvil de longitud `FirstLength`.
3. Calcular la diferencia entre el precio mediano y su media móvil.
4. Aplicar un segundo suavizado con una media móvil de longitud `SecondLength`.
5. Determinar la dirección de la tendencia comparando el valor suavizado actual con el anterior.
6. Generar señales cuando cambia la dirección:
   - Un giro hacia arriba por encima de cero abre una posición larga.
   - Un giro hacia abajo por debajo de cero abre una posición corta.

## Parámetros

- **Candle Type** – marco temporal de las velas procesadas.
- **First Length** – período para el suavizado del precio.
- **Second Length** – período para el suavizado de la señal.

La estrategia utiliza órdenes de mercado y funciona solo con velas completadas.
