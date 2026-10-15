# Estrategia de Lanzamiento de Moneda Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia de Lanzamiento de Moneda emula el asesor experto original de MetaTrader donde las entradas se determinan por un lanzamiento de moneda pseudoaleatorio. Solo se puede abrir una posición a la vez. Cada vela completada actúa como un punto de decisión: cuando la operación anterior está plana, la estrategia lanza una moneda y abre inmediatamente una posición larga o corta usando el volumen de operación calculado. Cada operación está protegida con niveles de stop loss y take profit, mientras que un stop de seguimiento opcional puede ajustar el riesgo a medida que el mercado se mueve a favor de la posición.

Se implementa un modelo de dimensionamiento de posición estilo martingale. Si la posición anterior fue detenida, la siguiente operación aumentará su tamaño por un multiplicador configurable. Las operaciones exitosas restablecen el volumen al tamaño base. Un volumen máximo definido por el usuario previene el crecimiento descontrolado del tamaño de la operación.

## Reglas de operación

1. En cada vela completada, la estrategia evalúa la posición actual.
2. Cuando no hay posición abierta, un número pseudoaleatorio selecciona la dirección larga o corta. Ambos lados tienen igual probabilidad.
3. Cada nueva operación usa el volumen base a menos que la operación anterior terminara con un stop loss. En ese caso, el volumen se multiplica por el factor martingale, respetando el límite de volumen máximo.
4. Los precios de stop loss y take profit de protección se adjuntan a cada posición. Cuando el precio de cierre alcanza esos umbrales, la posición se cierra con una orden de mercado.
5. El stop de seguimiento monitorea el movimiento favorable. Una vez que la ganancia supera la distancia de seguimiento más el paso, el nivel de stop se mueve hacia el precio para asegurar ganancias.

## Parámetros

- **Stop Loss** – distancia en pasos de precio usada para calcular el stop loss desde el precio de entrada.
- **Take Profit** – distancia en pasos de precio añadida al precio de entrada para el take profit.
- **Trailing Stop** – distancia de ganancia que activa el mecanismo de stop de seguimiento. Establecer en cero para deshabilitar el seguimiento.
- **Trailing Step** – ganancia adicional mínima requerida antes de que el stop de seguimiento se mueva de nuevo.
- **Base Volume** – volumen de la primera operación en un ciclo martingale.
- **Martingale Mult** – multiplicador aplicado al último volumen detenido para determinar el siguiente tamaño de orden.
- **Max Volume** – límite máximo para el tamaño de orden. Cuando se supera, la operación se omite y se registra una advertencia.
- **Candle Type** – serie de velas que define cuándo se ejecutan los lanzamientos de moneda y las verificaciones de gestión de riesgo.

## Notas

- La estrategia usa órdenes de mercado tanto para entradas como para salidas para imitar el comportamiento del asesor experto original.
- Los cálculos del stop de seguimiento dependen del paso de precio del instrumento. Si no hay un paso de precio disponible, se usan valores de puntos sin procesar en su lugar.
- Los números aleatorios se generan con una semilla determinista basada en la hora actual para evitar secuencias idénticas en ejecuciones simultáneas.
