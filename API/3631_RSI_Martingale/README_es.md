# Estrategia RSI Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
RSI Martingale es un puerto del MetaTrader 5 asesor experto `RSI&Martingale1.5`. La estrategia busca reversiones de impulso esperando hasta que el índice de fuerza relativa (RSI) alcance un valor extremo dentro de una ventana retrospectiva configurable. Cuando aparece un extremo, abre una operación en la dirección de la reversión media esperada y sale cuando RSI cruza la línea media de 50 o cuando se alcanza un objetivo fijo de parada/toma. Opcionalmente, un módulo martingala puede reabrir la posición en la dirección opuesta con un mayor volumen después de una operación perdedora. Los límites diarios de pérdidas y ganancias, junto con los filtros horarios, permiten suspender las operaciones durante sesiones más riesgosas o después de alcanzar los objetivos de preservación de capital.

## Lógica estratégica
### RSI extremos
* **Indicador**: un único RSI calculado sobre el tipo de vela seleccionado. Se debe formar el indicador (suficientes datos históricos) antes de considerar las operaciones.
* **Detección mínima**: si el último valor de RSI es menor o igual a cada valor de RSI dentro de la ventana `Bars For Extremes` configurada y el valor está por debajo de 50, la estrategia abre una posición larga.
* **Detección máxima**: si el último valor de RSI es mayor o igual a todos los valores dentro de la ventana retrospectiva y el valor es superior a 50, la estrategia abre una posición corta.

### Gestión de posiciones
* **Activador de salida**: las posiciones se cierran cuando RSI cruza la línea neutral 50 hacia el lado opuesto (los largos salen por encima de 50, los cortos salen por debajo de 50).
* **Objetivos fijos**: distancias opcionales de stop-loss y take-profit expresadas en pips. Cuando está habilitada, la estrategia compara el máximo/mínimo de la vela más reciente con esos precios objetivo y cierra la posición si se alcanza cualquiera de los niveles.
* **Alineación del volumen**: cada volumen de pedido está alineado con la configuración de paso, mínimo y máximo del valor antes del envío.

### Martingale recuperación
* **Activador**: después de que una posición se cierra con una ganancia negativa, la estrategia recuerda la dirección y el volumen de la operación perdedora.
* **Reingreso**: en la siguiente vela elegible, y solo si no hay ninguna posición abierta, puede abrir inmediatamente una operación en la dirección opuesta. El volumen es el volumen perdedor multiplicado por `Martingale Multiplier` o la base `Initial Volume` dependiendo del interruptor `Enable Martingale`.
* **Restablecer**: una vez enviada la orden de martingala, la información de pérdida almacenada se borra para evitar intentos repetidos.

### Control diario de capital
* **Línea de base**: la estrategia captura el capital de la cuenta al comienzo de cada día de negociación y restablece el indicador de suspensión.
* **Ventana de monitoreo**: los límites diarios se evalúan solo entre `Daily Control Start` y `Daily Control End` horas.
* **Suspensión**: si el capital crece más allá de `Daily Profit %` o cae por debajo de `Daily Loss %`, la estrategia cierra cualquier posición abierta y omite nuevas operaciones hasta el día siguiente.

### Filtros de sesión
* **Ventana de negociación**: se permiten nuevas posiciones solo cuando la hora actual es entre `Trading Start` y `Trading End` (inclusive).
* **Evitación de horas**: 24 parámetros booleanos reflejan la configuración de "evitación de noticias" de la fuente EA y bloquean el comercio durante las horas seleccionadas.

## Parámetros
* **Volumen inicial**: volumen de pedido base para entradas estándar.
* **RSI Período**: número de períodos utilizados por el indicador RSI.
* **Bars For Extremes**: cuántas velas terminadas se escanean cuando se busca el último RSI mínimo o máximo.
* **Take Profit (pips)** – distancia al take-profit fijo; configúrelo en `0` para deshabilitarlo.
* **Stop Loss (pips)** – distancia hasta el stop-loss fijo; configúrelo en `0` para deshabilitarlo.
* **Habilitar Martingale**: habilita el módulo de recuperación de martingala después de una operación perdedora.
* **Martingale Multiplicador**: multiplicador aplicado al volumen perdedor anterior cuando la martingala está activa.
* **Objetivos diarios**: alterna la lógica de suspensión de pérdidas/ganancias diarias.
* **% de beneficio diario**: porcentaje de beneficio que detiene las operaciones del día actual.
* **% de pérdida diaria**: porcentaje de pérdida que detiene la negociación del día actual.
* **Inicio del control diario / Fin del control diario**: límites de horas para evaluar los límites diarios.
* **Inicio de negociación/Fin de negociación**: límites horarios que permiten nuevas posiciones.
* **Evitar la hora 00... Evitar la hora 23**: deshabilita el comercio durante la hora correspondiente.
* **Tipo de vela**: suscripción de vela utilizada para el indicador RSI y todos los cálculos.

## Notas adicionales
* La estrategia opera únicamente con velas terminadas y no evalúa los ticks intrabar.
* Los cálculos de ganancias diarias combinan la estrategia realizada PnL con PnL flotante basada en el último precio de cierre.
* No hay ninguna implementación de Python para esta estrategia en el paquete; sólo se proporciona la versión C#.
