# Básico ATR Detener Tomar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Básico ATR Stop Tome los puertos del MetaTrader 4 asesor experto **“Básico ATR stop_take asesor experto»** a la StockSharp estrategia de alto nivel API. El sistema es intencionalmente mínimo: abre exactamente una posición de mercado en la dirección elegida, calcula un rango verdadero promedio (ATR) en las velas en funcionamiento y adjunta niveles protectores de stop-loss y take-profit derivados de los multiplicadores ATR. Una vez que la operación se cierra en cualquiera de los niveles, la estrategia se prepara inmediatamente para la siguiente configuración en la misma dirección.

## Lógica estratégica
### Fundación del indicador
* **Rango verdadero promedio (ATR)**: calculado según el tipo de vela suscrita con una mirada retrospectiva configurable. El indicador mide la volatilidad reciente y escala tanto la distancia de parada como la de objetivo.

### Reglas de entrada
* Se ejecuta al cierre de cada vela terminada después de que ATR esté completamente formado.
* Si no hay ninguna posición abierta y el parámetro de dirección está establecido en **Compra**, se envía una orden de compra de mercado utilizando el volumen configurado.
* Si no hay ninguna posición abierta y el parámetro de dirección está establecido en **Vender**, se envía una orden de venta de mercado con el volumen configurado.
* Al elegir **Ninguno** se deshabilitan las nuevas entradas y se mantienen administradas las posiciones existentes hasta que se cierran.

### reglas de salida
* **ATR stop-loss** – la distancia es igual a `ATR × Stop Factor`. Para posiciones largas, el stop se coloca debajo de la entrada; para pantalones cortos se coloca encima de la entrada. Cuando el extremo de la vela cruza el nivel, la posición se cierra en el mercado.
* **ATR obtención de beneficios**: la distancia es igual a `ATR × Take Factor`. Para posiciones largas, el objetivo de ganancias se sitúa por encima de la entrada; para pantalones cortos se coloca debajo. Alcanzar el nivel cierra la operación en el mercado.
* Si cualquiera de los multiplicadores se establece en `0`, el nivel correspondiente está deshabilitado; la estrategia continúa monitoreando el nivel restante si está presente.

### Gestión de posiciones
* Sólo se permite una posición a la vez. Después de una salida, la estrategia espera el cierre de la siguiente vela antes de volver a entrar en la misma dirección.
* `StartProtection()` se invoca durante el inicio para que las posiciones manuales externas sean monitoreadas por el subsistema de protección StockSharp.

## Parámetros
* **Dirección comercial**: lado del mercado para negociar (`None`, `Buy` o `Sell`).
* **Volumen comercial**: volumen de pedidos para la entrada al mercado único.
* **ATR Período**: número de velas utilizadas en el cálculo de ATR.
* **Factor de parada**: multiplicador ATR aplicado a la distancia de parada-pérdida. El cero desactiva la parada de protección.
* **Factor de toma**: multiplicador de ATR aplicado a la distancia de toma de ganancias. Cero desactiva el objetivo de ganancias.
* **Tipo de vela**: período de tiempo de las velas utilizadas para el cálculo de ATR y la gestión comercial.

## Notas adicionales
* Los parámetros predeterminados replican el comportamiento del EA (modo solo largo, volumen de lote 0,01, período ATR 14, factor de parada 1,5, factor de toma 2,0).
* Las comparaciones de precios utilizan máximos y mínimos de velas, lo que significa que los activadores de stop-loss y take-profit se producen tan pronto como se traspasa el nivel dentro del rango de la vela.
* La estrategia no acumula ni invierte posiciones; en cambio, siempre se aplana y espera a que se cierre la siguiente barra antes de realizar un nuevo pedido.
* En este paquete sólo se proporciona la implementación de C#; No existe una versión de Python para esta estrategia.
