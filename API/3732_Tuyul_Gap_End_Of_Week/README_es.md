# Brecha de Tuyul fin de semana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Tuyul Gap End Of Week transfiere el asesor experto MetaTrader 5 `TuyulGAP` a StockSharp. La estrategia se prepara para la apertura semanal del mercado escaneando un número configurable de velas recientes el viernes por la noche, colocando un par de órdenes stop de ruptura alrededor del máximo más alto y el mínimo más bajo. Sólo se permite una sesión de negociación por semana; Una vez que se organizan las órdenes, la estrategia espera a que el precio supere cualquiera de los niveles. Cualquier posición abierta que alcance un objetivo de ganancias seguro en la moneda de la cuenta se cierra inmediatamente y todas las órdenes pendientes restantes se cancelan el lunes para restablecer el flujo de trabajo para la próxima semana.

## Lógica estratégica
* **Activador de sesión semanal**: la configuración se ejecuta en un día laborable configurable (viernes de forma predeterminada) cuando el reloj del intercambio llega a la hora configurada. Durante la ventana de minutos (de 23:00 a 23:15 de forma predeterminada), la estrategia prepara los niveles de ruptura una vez por sesión.
* **Niveles de ruptura dinámica**: el máximo más alto y el mínimo más bajo de las velas terminadas `Lookback Bars` anteriores definen los precios de activación. Buy Stop se coloca un tic por encima del máximo, Sell Stop un tic por debajo del mínimo, imitando el desplazamiento del punto MetaTrader.
* **Higiene de orden pendiente**: si ya existe una orden suspendida para la semana, no se vuelve a crear. La orden pendiente opuesta permanece activa después de que se activa un lado, por lo que la estrategia puede operar en cualquier dirección de la brecha.
* **Salida segura de ganancias**: las posiciones abiertas se monitorean en cada vela terminada. Cuando el beneficio no realizado de una posición alcanza el objetivo de beneficio seguro (en la moneda de la cartera), se estabiliza en el mercado independientemente de la dirección.
* **Restablecimiento semanal**: en la primera vela del lunes, la estrategia cancela cualquier orden pendiente aún activa y vuelve a armar el indicador de sesión para que se pueda organizar la configuración del próximo viernes.

## Parámetros
* **Volumen**: volumen de órdenes para las órdenes stop de ruptura.
* **Stop Loss (puntos)** – distancia desde el precio de entrada, expresada en puntos del instrumento, utilizada para colocar un stop de protección después de la apertura de una posición. Establezca en `0` para desactivar la parada.
* **Barras retrospectivas**: número de velas terminadas inspeccionadas para calcular los niveles máximos y mínimos semanales.
* **Configuración del día de la semana**: índice del día (0=domingo… 6=sábado) que activa la configuración semanal. El valor predeterminado de `5` mantiene el comportamiento original del viernes.
* **Hora de configuración**: hora de intercambio utilizada como ancla para organizar las órdenes de ruptura.
* **Ventana de minutos de configuración**: número de minutos después de `Setup Hour` cuando la configuración sigue siendo válida. Con el valor predeterminado `15` la estrategia se ejecuta entre las 23:00 y las 23:15 inclusive.
* **Objetivo de beneficio seguro**: beneficio mínimo no realizado por posición (en la moneda de la cartera) que desencadena una salida inmediata del mercado.
* **Tipo de vela**: período de tiempo utilizado para el escaneo alto/bajo y el bucle de monitoreo.

## Notas adicionales
* La orden de stop loss se envía solo después de que se abre una posición, porque StockSharp no admite adjuntar un stop de protección directamente a una orden de stop pendiente.
* Los niveles de volumen, precio y parada se normalizan utilizando la información de precisión y pasos del valor que proporciona StockSharp.
* No existe una traducción de Python para esta estrategia; En este paquete solo se incluye la implementación de C#.
