# Estrategia AMkA Signal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia utiliza la derivada de la Media Móvil Adaptativa de Kaufman (KAMA) combinada con un filtro de volatilidad basado en la desviación estándar. Se abre una posición larga cuando la tasa de cambio de KAMA supera un umbral dinámico; se abre una posición corta cuando cae por debajo del umbral negativo. El umbral se calcula multiplicando la desviación estándar de los cambios de KAMA por un factor definido por el usuario.

## Parámetros

- **KAMA Length** – período de retrospección para el indicador KAMA.
- **Fast Period** – período rápido de EMA utilizado en el suavizado de KAMA.
- **Slow Period** – período lento de EMA utilizado en el suavizado de KAMA.
- **Deviation Multiplier** – multiplicador aplicado a la desviación estándar para formar el umbral de señal.
- **Take Profit** – porcentaje para la fijación automática de beneficios.
- **Stop Loss** – porcentaje para el stop de protección.
- **Candle Type** – marco temporal de las velas utilizadas para los cálculos.

## Lógica de trading

1. Suscribirse a velas del marco temporal seleccionado.
2. Calcular KAMA para cada vela y calcular su cambio respecto al valor anterior.
3. Actualizar el indicador de desviación estándar con los valores de cambio.
4. Cuando el cambio supera `Deviation Multiplier * StdDev`, abrir o cerrar posiciones:
   - Si el cambio es mayor que el umbral: cerrar posiciones cortas y abrir larga.
   - Si el cambio es menor que el umbral negativo: cerrar posiciones largas y abrir corta.
5. Las órdenes de protección para take profit y stop loss se gestionan automáticamente con `StartProtection`.

## Notas

La estrategia trabaja únicamente con velas completadas y usa tabulaciones para la indentación en el código fuente. Todos los comentarios están escritos en inglés según lo requerido.
