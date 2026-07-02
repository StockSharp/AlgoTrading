# Donchian revendedor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Donchian Scalper es una StockSharp versión del MetaTrader 4 asesor experto `DonchianScalperEA`. La estrategia monitorea los límites del canal Donchian y la media móvil exponencial (EMA) de la misma longitud. Una orden de parada pendiente se activa solo después de que el precio retrocede a través del EMA, lo que indica que el impulso se ha restablecido antes de una posible ruptura. Las entradas se ejecutan con órdenes stop colocadas en los extremos Donchian actuales y protegidas por la banda opuesta. Las ganancias se gestionan mediante una distancia fija de obtención de ganancias o mediante topes dinámicos adaptativos que siguen la estructura de mercado elegida.

## Lógica estratégica
### Preparación de entrada
* **Validación de retroceso**: la estrategia espera hasta que una de las dos velas previamente cerradas cruce por debajo de EMA (para largos) o por encima de EMA (para cortos). El nivel de cruce se compensa con la distancia configurable *Cross Anchor* para garantizar que el retroceso sea significativo.
* **Armado de ruptura**: una vez que se cumple la condición de retroceso y el temporizador de enfriamiento ha expirado, se envía una orden de detención en el límite Donchian más reciente (banda superior para posiciones largas, banda inferior para posiciones cortas). La banda opuesta define la parada de protección inicial. Las órdenes pendientes existentes se realinean automáticamente cuando los niveles de Donchian se aplanan durante al menos dos velas.

### Gestión comercial
* **Protección inicial**: cuando se ejecuta una orden de ruptura, la estrategia coloca una orden de parada de protección utilizando el precio de parada precalculado. El nivel de parada es igual a la banda opuesta Donchian y se puede desplazar hacia adentro mediante la configuración *Stop Loss (puntos)*.
* **Control de beneficios**: hay dos modos de gestión disponibles:
  * *Cerrar con ganancias*: cierra la posición una vez que el movimiento neto del precio de entrada promedio excede la distancia de obtención de ganancias configurada.
  * *Trailing* – mantiene la operación abierta y periódicamente ajusta el stop de protección. El motor de seguimiento puede seguir el límite Donchian, el EMA o una banda de volatilidad basada en ATR.
* **Enfriamiento**: después de cerrar todas las posiciones, la estrategia espera el número especificado de velas terminadas antes de armar nuevas órdenes de ruptura. Esto reproduce la lógica MetaTrader que requiere al menos tres barras entre operaciones.

## Parámetros
* **Volumen**: volumen de órdenes utilizado para detener las entradas y salidas del mercado.
* **Período del canal**: longitud del canal Donchian, también utilizado para el filtro EMA.
* **Ancla cruzada**: distancia adicional (en puntos) que el retroceso debe exceder antes de que se active la orden de ruptura.
* **Stop Loss (puntos)** – distancia agregada a la banda opuesta Donchian para la parada de protección inicial; configúrelo en `0` para colocar la parada directamente en la banda.
* **Take Profit (puntos)**: objetivo de ganancias utilizado por el modo *Close At Profit*. Se ignora cuando el modo de seguimiento está activo.
* **Tipo de vela**: cálculos del indicador de conducción en el marco temporal.
* **Modo de beneficio**: selecciona entre la salida de obtención de beneficios fija y los topes dinámicos adaptativos.
* **Modo de seguimiento**: motor de seguimiento utilizado en el modo de ganancias *Trailing*. Las opciones son límite Donchian, EMA o seguimiento basado en ATR.
* **Barras de enfriamiento**: número mínimo de velas terminadas que deben pasar después de que la posición se estabilice antes de que se puedan realizar nuevas órdenes.
* **ATR Periodo / ATR Multiplicador**: parámetros para el motor de seguimiento ATR. El multiplicador define cuántos ATR se restan (largos) o se suman (cortos) para calcular el trailing stop.

## Notas adicionales
* La estrategia alinea cada precio de parada y entrada con el paso de precio del instrumento para garantizar el cumplimiento del intercambio.
* Cuando tanto las órdenes stop largas como las cortas están activas, al ejecutar una de las partes se cancelará automáticamente la orden pendiente opuesta para evitar la cobertura.
* Si *Take Profit (puntos)* se establece en cero mientras el modo de ganancias permanece *Close At Profit*, la estrategia mantendrá las posiciones abiertas hasta que se alcance el tope de protección.
* La conversión se centra en el StockSharp API de alto nivel: vinculación de indicadores, suscripciones de velas y métodos auxiliares (`BuyStop`, `SellStop`, `SellMarket`, etc.). La implementación de Python no está incluida en este paquete.
