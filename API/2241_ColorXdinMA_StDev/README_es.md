# Estrategia ColorXdinMA con Desviación Estándar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un puerto de StockSharp del experto MQL5 **Exp_ColorXdinMA_StDev**.
Combina dos medias móviles en una única línea llamada `XdinMA` y rastrea su
cambio a lo largo del tiempo. La diferencia entre el valor actual y el anterior de `XdinMA`
se compara con un múltiplo de su desviación estándar reciente. Cuando el
cambio supera el umbral positivo se abre una posición larga, mientras que una caída
por debajo del umbral negativo abre una posición corta.

## Cómo funciona

1. Se calculan dos medias móviles simples:
   - **Main MA** – período definido por `MainLength`.
   - **Plus MA** – período definido por `PlusLength`.
2. Se construye la línea personalizada `XdinMA = 2 * MainMA - PlusMA`.
3. El cambio de `XdinMA` entre velas consecutivas se pasa a un indicador de desviación estándar con longitud `StdPeriod`.
4. Si el cambio es mayor que `K1 * StdDev`, se coloca una orden de compra. Si es menor que `-K1 * StdDev`, se coloca una orden de venta. Las posiciones opuestas existentes se cierran antes de abrir una nueva.

## Parámetros

| Parámetro   | Descripción                                        |
|-------------|----------------------------------------------------|
| `MainLength`| Período de la media móvil principal.               |
| `PlusLength`| Período de la media móvil secundaria.              |
| `StdPeriod` | Número de barras usadas para la desviación estándar. |
| `K1`        | Multiplicador para el umbral de desviación.        |
| `K2`        | Reservado para futura extensión del segundo filtro.|

Todos los parámetros están expuestos a través de `StrategyParam` para que puedan optimizarse o
cambiarse desde la interfaz de usuario.

## Notas

- Solo se procesan velas completadas.
- La estrategia usa órdenes de mercado y no implementa lógica de stop-loss o
  take-profit.
- El gráfico incluye ambas medias móviles y las operaciones ejecutadas para análisis
  visual.
