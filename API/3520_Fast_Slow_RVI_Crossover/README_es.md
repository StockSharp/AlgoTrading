# Estrategia de cruce de RVI rápido y lento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el MetaTrader asesor experto `_HPCS_FastSlowRVIsCrossOver_MT4_EA_V01_WE`. Se negocia cuando la línea principal del Índice de Vigor Relativo (RVI) cruza su línea de señal durante la sesión de negociación configurada. Solo se permite una operación por vela, y la estrategia admite distancias opcionales de stop loss, takeprofit y trailing stop expresadas en pips.

## Lógica de trading
1. Cree velas estándar basadas en el tiempo seleccionadas mediante el parámetro **Tipo de vela**.
2. Calcule el RVI con el **Período RVI** configurado y un promedio móvil simple de 4 períodos como línea de señal.
3. Cuando el RVI se eleva por encima de la línea de señal, cierre cualquier posición corta y abra/escale a una posición larga.
4. Cuando el RVI cae por debajo de la línea de señal, cierre cualquier posición larga y abra/escale a una posición corta.
5. Ignore las señales que aparecen fuera del intervalo **Hora de inicio** y **Hora de finalización**.
6. Colocar órdenes de protección según los parámetros de riesgo seleccionados. Las paradas dinámicas son administradas por el motor de protección StockSharp.
7. Evite entradas duplicadas en la misma vela reaccionando solo una vez por barra.

## Parámetros
| Nombre | Descripción |
|------|-------------|
| **Período RVI** | Número de barras utilizadas por el Índice de Vigor Relativo. |
| **Obtener ganancias (pips)** | Distancia de toma de ganancias opcional medida en pips. Establezca en cero para desactivar. |
| **Detener pérdidas (pips)** | Distancia de stop-loss opcional medida en pips. Establezca en cero para desactivar. |
| **Parada dinámica (pips)** | Distancia de trailing stop opcional en pips. Establezca en cero para desactivar el seguimiento. |
| **Paso final (pips)** | Se requiere un movimiento mínimo favorable antes de que se ajuste el trailing stop. Funciona sólo cuando el trailing stop está activo. |
| **Volumen** | Volumen de pedidos presentado en cada entrada. |
| **Tipo de vela** | Marco de tiempo o tipo de datos de vela personalizados utilizados para el análisis. |
| **Hora de inicio** | Inicio de la ventana de negociación diaria (inclusive). |
| **Tiempo de parada** | Fin de la ventana de negociación diaria (exclusivo). |

## Notas
- El tamaño del pip se adapta al tamaño del tick de seguridad para que coincida con el manejo de puntos MetaTrader (los símbolos de 5 y 3 dígitos usan un multiplicador de 10×).
- Llame a `StartProtection` una vez dentro de `OnStarted` para habilitar las órdenes de protección y la gestión de seguimiento.
- Todos los comentarios en el código fuente están escritos en inglés, como lo exigen las pautas del proyecto.
