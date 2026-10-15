# Estrategia ColorXvaMA Digit StDev
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia opera basándose en qué tan lejos se desvía el precio de una media móvil exponencial (EMA). Dos multiplicadores de desviación (K1 y K2) definen bandas interiores y exteriores calculadas a partir de la desviación estándar del precio.

Cuando el precio sube por encima de la EMA en K2 desviaciones estándar, la estrategia entra en una posición larga. Cuando el precio cae por debajo de la EMA en K2 desviaciones estándar, entra en una posición corta. Las posiciones existentes se cierran una vez que la desviación regresa dentro de la banda interior definida por K1.

## Parámetros
- **EMA Length** – período de la media móvil exponencial.
- **StdDev Length** – período para el cálculo de la desviación estándar.
- **Deviation K1** – multiplicador de la banda interior usado para salir de posiciones.
- **Deviation K2** – multiplicador de la banda exterior usado para abrir posiciones.
- **Candle Type** – marco temporal de las velas.

## Indicadores
- Exponential Moving Average
- StandardDeviation

## Cómo funciona
1. Suscribirse a velas del marco temporal elegido.
2. Calcular EMA y desviación estándar del precio.
3. Calcular la desviación del precio respecto a la EMA.
4. Entrar largo/corto cuando la desviación supera ±K2×StdDev.
5. Salir cuando la desviación regresa dentro de ±K1×StdDev.

Este enfoque busca capturar fuertes desviaciones de la media y salir en la reversión.
