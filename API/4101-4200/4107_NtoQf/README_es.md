# Multifiltro NTOqF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia NTOqF Multi-Filter traslada el asesor experto MetaTrader 4 "NTOqF" (versiones V1 a V3) al API de alto nivel de StockSharp. El robot original combina múltiples osciladores y filtros de seguimiento de tendencias, cada uno de los cuales se puede habilitar o deshabilitar de forma independiente. Esta versión de C# conserva la misma capacidad de configuración, admite períodos de tiempo separados para cada indicador y aplica la gestión comercial a través de paradas fijas, objetivos de obtención de ganancias y una parada móvil opcional expresada en pips.

## Lógica estratégica
### Filtros de indicador
* Filtro **RSI**: genera una señal larga cuando el valor RSI (en el turno configurado) está por debajo de `RSI Lower` y una señal corta cuando el valor está por encima de `RSI Upper`. Las lecturas neutrales cancelan las entradas.
* Filtro **Stochastic**: compara %K y %D. Cuando `Use Stochastic High/Low` está habilitado, la línea principal también debe estar por encima de `Stoch High` para largos o por debajo de `Stoch Low` para cortos; de lo contrario, se utilizan cruces %K/%D simples.
* Filtro **ADX**: utiliza +DI frente a –DI para determinar la dirección. Cuando la opción `Use ADX Main` está habilitada, la línea principal ADX debe exceder `ADX Main` antes de que se acepte cualquier entrada.
* **Parabolic SAR filtro**: interpreta el valor SAR relativo al cierre de la barra seleccionada. Los valores por encima del precio favorecen las posiciones largas (reflejando el comportamiento en el código MQL), los valores por debajo favorecen las posiciones cortas.
* **Filtro de media móvil**: compara la media móvil seleccionada (con cambio positivo opcional) con el precio de cierre en el cambio base. El precio por encima del MA favorece las posiciones largas; El precio por debajo favorece los pantalones cortos.

Todos los filtros habilitados deben coincidir en la misma dirección. Si algún filtro devuelve un estado neutral (por ejemplo, RSI permanece entre sus umbrales), no se abre ninguna posición.

### Reglas de entrada
* Las señales se evalúan en el marco temporal de negociación principal (`Candle Type`).
* Sólo se permite una posición a la vez; la estrategia espera a que se cierre la posición anterior antes de entrar en una nueva.
* El volumen del pedido se toma de `Trade Volume` (lotes).

### reglas de salida
* **Stop de pérdidas/toma de ganancias fijo**: expresado en pips y convertido en compensaciones de precios utilizando el tamaño de paso del instrumento. Establezca un parámetro en `0` para desactivar el nivel correspondiente.
* **Trailing stop**: cuando está activado, el stop se sigue una vez que el beneficio no realizado supera la distancia de seguimiento y el stop actual se retrasa en más de esa distancia. Las posiciones largas mueven el stop hacia arriba, las posiciones cortas lo mueven hacia abajo.

### Comportamiento en múltiples períodos de tiempo
Cada indicador puede suscribirse a su propio marco temporal. Un valor de período de tiempo de `0` reutiliza el período de tiempo de negociación principal, mientras que los valores positivos representan suscripciones de `TimeFrameCandle` basadas en minutos. Los valores del indicador se evalúan únicamente en velas completadas y respetan el parámetro `Shift` para que la estrategia pueda reflejar el comportamiento de "revisión" del experto original MetaTrader.

## Parámetros
* **Tipo de vela**: plazo de negociación utilizado para impulsar las ejecuciones.
* **Volumen** – volumen de órdenes de mercado (lotes).
* **Take Profit (pips)** – objetivo de ganancias; `0` se desactiva.
* **Stop Loss (pips)** – parada de protección; `0` se desactiva.
* **Usar Trailing** / **Trailing Stop (pips)**: habilita y dimensiona el trailing stop.
* **Shift** – número de velas completadas hacia atrás al leer los valores y el precio del indicador.
* **RSI parámetros**: alternancia, período, umbrales superior/inferior y período de tiempo.
* **Stochastic parámetros**: alternar, %K/%D/duraciones de desaceleración, niveles de confirmación alto/bajo opcionales y período de tiempo.
* **ADX parámetros**: alternancia, período, período de tiempo DI, umbral de línea principal opcional y período de tiempo principal.
* **Parabolic SAR parámetros**: alternar, paso de aceleración, aceleración máxima y período de tiempo.
* **Parámetros de media móvil**: alternancia, período, desplazamiento adicional aplicado al búfer MA, método de promediación (SMA/EMA/SMMA/LWMA), precio aplicado y período de tiempo.

## Notas
* Las colas de indicadores respetan el `Shift` configurado, lo que garantiza que las señales se basen en valores históricos de la misma manera que el experto MQL.
* La lógica de seguimiento solo se activa una vez que la operación ya tiene ganancias por más de la distancia de seguimiento y la parada está a más de esa distancia del precio, coincidiendo con el comportamiento del EA original.
* No se proporciona ninguna versión de Python para este paquete de estrategia.
