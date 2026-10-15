# Estrategia BreakOut15
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
BreakOut15 es una estrategia de ruptura de 15 minutos convertida del asesor experto MetaTrader 4 "BreakOut15.mq4". La estrategia combina un filtro cruzado de media móvil con ejecución de ruptura y protección de seguimiento de múltiples etapas. Los pedidos se envían a través del StockSharp API de alto nivel y dependen únicamente de velas terminadas.

## Lógica principal
1. Calcule dos promedios móviles configurables (rápido y lento) utilizando el método, período, turno y precio aplicado seleccionados.
2. Cuando el promedio rápido supere el promedio lento, programe un precio de ruptura largo en `Close + BreakoutLevel * PriceStep`. Un cruce bajista programa una breve ruptura en `Close - BreakoutLevel * PriceStep`.
3. Los precios de ruptura pendientes se cancelan si la condición de cruce desaparece, finaliza el horario de negociación o se activa una ruptura en la dirección opuesta.
4. Las entradas al mercado se ejecutan una vez que la vela atraviesa el nivel pendiente y se pasan los controles de riesgo y equidad.
5. Las posiciones abiertas se gestionan mediante stop-loss, take-profit y uno de los tres modos de trailing-stop. Los cruces de media móvil obligan a una salida inmediata.
6. Los filtros de tiempo opcionales evitan nuevas operaciones fuera de la ventana configurada y pueden liquidar posiciones a última hora de los viernes.

## Gestión monetaria
* **UseMoneyManagement / TradeSizePercent**: permite el dimensionamiento basado en el riesgo. El tamaño de la posición es igual a la parte entera de `floor(equity * percent / 10000) / 10`, con un mínimo de 1 lote.
* **FixedVolume**: tamaño de reserva cuando la administración del dinero está deshabilitada o el capital no está disponible.
* **MaxVolume**: limita cualquier volumen calculado.
* **MinimumEquity**: bloquea nuevas operaciones cuando el capital cae por debajo del umbral.

## Gestión del riesgo
* **StopLossPips / TakeProfitPips**: compensaciones protectoras clásicas medidas en pips (convertidas mediante el paso del precio del instrumento).
* **UseTrailingStop**: activa el manejo de parada dinámica una vez que existe una posición.
* **Tipo de parada final**
  * `Immediate`: recorra la distancia original de stop-loss de inmediato.
  * `Delayed`: espere `TrailingStopPips` de ganancias antes de seguir esa distancia.
  * `MultiLevel`: bloquea las ganancias en tres hitos programables (`Level1/2/3TriggerPips`) y luego avanza por `Level3TrailingPips`.

## Horario de negociación
* **UseTimeLimit, StartHour, StopHour**: permite operar solo dentro del intervalo de horas especificado.
* **UseFridayClose, FridayCloseHour**: opcionalmente, aplana todas las posiciones a última hora del viernes.

## Indicadores y datos
* **Promedios móviles rápido/lento**: elija entre los métodos simple, exponencial, suavizado, ponderado lineal o mínimos cuadrados.
* **Modos de precios aplicados**: reproduce fuentes de precios MT4 (cierre, apertura, máximo, mínimo, mediana, típico, ponderado).
* **CandleType**: el valor predeterminado es velas con un período de tiempo de 15 minutos, pero se puede cambiar si es necesario.

## Notas adicionales
* La estrategia sincroniza automáticamente los precios de entrada, stop y objetivo con el precio promedio actual de la posición, de modo que los ajustes finales reflejen las ejecuciones reales.
* Todos los cálculos dependen del instrumento `PriceStep`; asegúrese de que coincida con el mercado comercializado.
* Las pruebas deberían validar la activación de rupturas, las transiciones de trailing-stop y las reglas de redondeo de gestión del dinero en escenarios alcistas y bajistas.
