# Estrategia TCPivotLimit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera alrededor de los niveles clásicos de puntos de pivote diarios. Los puntos de pivote se calculan a partir del máximo, mínimo y precios de cierre del día anterior. Se colocan órdenes limitadas en los niveles de soporte o resistencia seleccionados y las posiciones se gestionan con niveles predefinidos de stop-loss y take-profit.

## Parámetros
- **Volume** – volumen de la orden.
- **Target Variant** – selecciona qué niveles de soporte/resistencia se utilizan para la entrada, el stop y el objetivo:
  1. Entrada en S1/R1, stop en S2/R2, objetivo en R1/S1.
  2. Entrada en S1/R1, stop en S2/R2, objetivo en R2/S2.
  3. Entrada en S2/R2, stop en S3/R3, objetivo en R1/S1.
  4. Entrada en S2/R2, stop en S3/R3, objetivo en R2/S2.
  5. Entrada en S2/R2, stop en S3/R3, objetivo en R3/S3.
- **Intraday Close** – cerrar cualquier posición abierta a las 23:00.
- **Modify Stop Loss** – mover el stop loss al primer nivel objetivo una vez alcanzado.

## Lógica de operación
1. Al inicio de cada día, la estrategia calcula el pivote y tres niveles de resistencia y tres de soporte usando los datos del día anterior.
2. Cuando el precio toca el nivel de soporte o resistencia elegido, se envía una orden limitada en la dirección opuesta.
3. La posición se cierra cuando se alcanza el nivel de stop-loss o take-profit. La modificación opcional del stop-loss puede reducir el riesgo tras alcanzar el primer objetivo.
4. Si *Intraday Close* está activado, cualquier posición abierta se cierra al final de la sesión de trading.
