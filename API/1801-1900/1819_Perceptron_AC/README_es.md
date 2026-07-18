# Estrategia Perceptron AC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un perceptrón simple sobre el Accelerator Oscillator (AC).
El valor de AC de la vela actual y de tres desplazamientos pasados se multiplican por pesos ajustables.
La suma de estos productos forma la salida del perceptrón que determina la dirección de la operación.

## Cómo funciona

1. Calcular el Accelerator Oscillator (AC) a partir de la diferencia entre el Awesome Oscillator y su SMA de 5 períodos.
2. Almacenar los últimos 22 valores de AC para acceder a desplazamientos de 0, 7, 14 y 21 barras.
3. Calcular la salida del perceptrón:
   `P = (X1-100)*AC[0] + (X2-100)*AC[7] + (X3-100)*AC[14] + (X4-100)*AC[21]`.
4. Si `P > 0` abrir o mantener una posición larga; si `P < 0` abrir o mantener una posición corta.
5. Cuando una posición gana al menos `StopLoss` puntos más allá del nivel de stop inicial:
   - Si el perceptrón cambia de dirección, invertir la posición.
   - De lo contrario, trailar el stop al nuevo precio menos/más `StopLoss`.

## Parámetros

- **X1** – peso para el valor AC actual (predeterminado 288).
- **X2** – peso para AC de 7 barras atrás (predeterminado 216).
- **X3** – peso para AC de 14 barras atrás (predeterminado 144).
- **X4** – peso para AC de 21 barras atrás (predeterminado 72).
- **Stop Loss** – umbral de seguimiento e inversión en unidades de precio (predeterminado 300).
- **Volume** – volumen de la orden (predeterminado 1).
- **Candle Type** – serie de velas a la que suscribirse (predeterminado 5 minutos).

## Reglas de trading

- Entrar largo cuando `P > 0` y no hay posición abierta.
- Entrar corto cuando `P < 0` y no hay posición abierta.
- Para posiciones abiertas, mover el stop-loss después de que el precio se mueva `Stop Loss * 2` en beneficio.
- Invertir la posición si la salida del perceptrón cambia de signo en ese momento.

## Versión original

Convertido del script MQL4 `auto_m5.mq4` ubicado en `MQL/11102`.
