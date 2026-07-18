# Estrategia MACD con Redes Neuronales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina un filtro de perceptrón simple de cuatro pesos con un cruce clásico de MACD. Solo se abre una posición cuando tanto el MACD como la red neuronal coinciden en la dirección.

## Cómo funciona

1. **Filtro de perceptrón**  
   Tres perceptrones evalúan el momentum del precio usando las diferencias entre el cierre actual y una serie de precios de apertura pasados. Cada perceptrón tiene cuatro pesos enteros (`X11`…`X34`) donde `0` significa sin influencia. La salida del perceptrón es una suma ponderada de las diferencias de precio.  
   Dependiendo del parámetro `Pass`, uno, dos o los tres perceptrones participan en la toma de decisiones. El filtro también define las distancias de stop-loss y take-profit (`Sl1`, `Tp1`, `Sl2`, `Tp2`).
2. **Confirmación MACD**  
   Se calcula un MACD estándar (12, 26, 9). Aparece una señal de compra cuando la línea MACD está por debajo de cero y cruza la línea de señal hacia arriba. Una señal de venta se produce cuando la línea está por encima de cero y cruza la línea de señal hacia abajo.
3. **Ejecución de operaciones**  
   - Se abre una posición larga si tanto el MACD como el filtro de perceptrón son positivos.  
   - Se abre una posición corta si ambos son negativos.  
   La posición se cierra cuando se alcanza el nivel de stop-loss o take-profit.

## Parámetros

| Nombre | Descripción |
| ------ | ----------- |
| `X11…X34` | Pesos para las entradas del perceptrón. |
| `Tp1`, `Sl1` | Take-profit y stop-loss para el primer perceptrón. |
| `Tp2`, `Sl2` | Take-profit y stop-loss para el segundo perceptrón. |
| `P1`, `P2`, `P3` | Desplazamientos en barras usados para calcular las entradas del perceptrón. |
| `Pass` | Número de perceptrones a usar (1-3). |
| `CandleType` | Serie de velas para los cálculos. |

