# Cajero automático 5 min Legacy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Cash Machine 5 min Legacy es una versión StockSharp del MetaTrader 4 asesores expertos `CashMachine_5min`. El sistema reacciona a las inversiones de impulso detectadas por el oscilador DeMarker y el oscilador rápido Stochastic en velas de cinco minutos. Una vez abierta una posición, la estrategia oculta sus niveles protectores de stop-loss y take-profit, revelándolos sólo a la lógica interna, de modo que los stop-loss del broker no son visibles. Las ganancias se protegen de forma incremental a través de tres hitos definidos por el usuario.

## Lógica estratégica
### Condiciones de entrada
* **Configuración larga**: espere a que el valor de DeMarker supere el umbral de 0,30 mientras la línea Stochastic %K cruza simultáneamente por encima de 20. Ambas condiciones deben cambiar el estado de la vela terminada anterior a la actual. Cuando es plana, la estrategia compra en el mercado utilizando el volumen de orden configurado.
* **Configuración corta** – espejo del caso largo: DeMarker debe caer por debajo de 0,70 y Stochastic %K debe cruzar por debajo de 80. La señal es válida sólo cuando la vela anterior estaba en el lado opuesto de ambos límites. La estrategia vende en corto por mercado cuando no hay ninguna posición abierta.

### Gestión comercial
* **Límites de riesgo ocultos**: una posición larga se cierra si el precio cae una distancia de `Hidden Stop Loss` o sube una distancia de `Hidden Take Profit`. Los pantalones cortos utilizan condiciones simétricas con los límites invertidos. Los niveles se monitorean internamente sin colocar órdenes stop reales.
* **Parada móvil por etapas**: tres puntos de control de toma de ganancias (`Target TP1`, `Target TP2`, `Target TP3`) ajustan la parada a medida que avanza el precio. Para posiciones largas, una vez que el precio alcanza un punto de control, el stop se eleva hasta el máximo de la vela menos `(target − 13)` pips. Para los cortos, el stop se reduce al mínimo de la vela más `(target + 13)` pips. Cada etapa se aplica sólo una vez y nunca se afloja.
* **Ejecución dinámica** – después de que se arma al menos una etapa, al tocar el stop dinámico se cierra la posición por orden de mercado.

### Mecánica de apoyo
* La estrategia estima automáticamente el tamaño del pip a partir del paso del precio del valor, y admite símbolos forex de 4/2 dígitos y 5/3 dígitos.
* Los cálculos y las señales del indicador se basan en el tipo de vela seleccionable (velas de cinco minutos de forma predeterminada). Sólo se procesan velas terminadas.

## Parámetros
* **Take Profit oculto**: distancia de toma de ganancias oculta en pips (predeterminado: `60`).
* **Stop Loss oculto**: distancia del stop-loss oculto en pips (predeterminado: `30`).
* **Objetivo TP1 / TP2 / TP3**: hitos de ganancias en pips que arman el trailing stop por etapas (predeterminado: `20`, `35`, `50`).
* **Volumen de órdenes**: volumen de órdenes de mercado utilizado para las entradas (predeterminado: `0.2`).
* **Longitud de DeMarker**: período promedio para el oscilador DeMarker (predeterminado: `14`).
* **Stochastic Longitud**: retrospectiva base para el oscilador Stochastic (predeterminado: `5`).
* **Stochastic %K** – factor de suavizado para la línea %K (predeterminado: `3`).
* **Stochastic %D** – factor de suavizado para la línea %D (predeterminado: `3`).
* **Tipo de vela**: período de tiempo utilizado para calcular los indicadores (predeterminado: velas de cinco minutos).

## Notas adicionales
* La estrategia abre sólo una posición a la vez y no se revertirá inmediatamente; espera a que se cierre la operación actual antes de actuar sobre una nueva señal.
* Los niveles de protección se aplican en el código a través de salidas del mercado, por lo que no hay órdenes stop pendientes en el libro de órdenes.
* El paquete contiene sólo la implementación de C#; no se proporciona ninguna versión de Python.
