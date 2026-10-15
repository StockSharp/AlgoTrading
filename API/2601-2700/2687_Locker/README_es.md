# Estrategia Locker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de cobertura basada en cuadrícula que alterna órdenes de mercado largas y cortas para bloquear pérdidas flotantes y capturar un pequeño porcentaje de ganancia sobre el saldo de la cuenta.

## Lógica de trading
* Abre la primera posición larga con el volumen inicial configurado tan pronto como se cierra la primera vela.
* Rastrea cada entrada posterior y mantiene un libro interno de tramos de compra y venta para estimar la ganancia combinada no realizada y realizada.
* Si el número de tramos activos llega a ocho, la estrategia cierra el par de compra/venta más antiguo disponible para mantener la exposición bajo control antes de hacer cualquier otra cosa en esa vela.
* Cuando la ganancia combinada sube por encima del porcentaje objetivo del valor de la cartera, cierra todas las posiciones restantes y restablece el estado interno.
* Cuando la ganancia combinada cae por debajo del objetivo negativo, mide la distancia entre el último precio de entrada y el precio de mercado actual. Si el precio ha subido por el paso configurado, añade un nuevo tramo corto; si el precio ha bajado la misma distancia, añade un nuevo tramo largo.
* Cada cierre utiliza órdenes de mercado en la dirección opuesta a la entrada registrada para que la cobertura se neutralice de inmediato.

## Parámetros
* **Profit %** – porcentaje del valor actual de la cartera que se debe bloquear antes de aplanar el libro.
* **Start Volume** – cantidad utilizada para la primera entrada larga que inicializa la cuadrícula.
* **Step Volume** – cantidad enviada para cada orden de cobertura una vez que se supera el umbral de pérdida.
* **Step Points** – número de pasos de precio entre niveles de cuadrícula; multiplicado por el paso de precio del instrumento para calcular la distancia de precio real.
* **Enable Automation** – interruptor maestro que pausa toda la lógica de trading cuando está desactivado.
* **Candle Type** – serie de velas utilizada para activar la lógica de decisión en cada barra terminada.

La conversión replica la lógica del experto original de MetaTrader adaptando la colocación de órdenes a la API de alto nivel de StockSharp y almacenando el estado detallado de la operación dentro de la estrategia para que el cálculo de ganancias coincida con la versión MQL.
