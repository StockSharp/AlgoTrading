# Estrategia de bisturí ZigAndZag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
ZigAndZagScalpelStrategy es un puerto StockSharp del kit de herramientas MetaTrader 4 "ZigAndZag" (carpeta 8304).
El paquete original combina un indicador personalizado y un asesor experto. Se utilizan dos ventanas ZigZag:

* **KeelOver**: un detector de oscilación retrospectivo que marca la tendencia dominante.
* **Slalom**: un breve detector de oscilación retrospectiva que define rupturas procesables.

Cuando el ZigZag a largo plazo gira hacia arriba, la estrategia busca el próximo mínimo de Slalom y espera el precio.
para elevar un número configurable de puntos por encima de ese pivote. Una orden de compra de mercado se emite una vez que
se alcanza la distancia de ruptura. Una regla simétrica abre una posición corta cuando cambia la tendencia KeelOver
baja, el Slalom alcanza un nuevo máximo y el precio cae por debajo de él. Las posiciones se pueden cerrar opcionalmente.
tan pronto como se confirma el pivote de Slalom opuesto, imitando la eliminación de la flecha de límite del indicador.

La implementación mantiene el limitador de comercio diario del asesor experto. Sólo un número configurable
Se permite un número de operaciones por día de negociación, restableciéndose automáticamente a la medianoche (hora de cambio). esto
reproduce la bandera de "nuevo día" del código original.

## como funciona
1. Suscríbase al flujo de velas principal definido por `CandleType`.
2. Alimenta dos instancias `ZigZagIndicator`:
   * Profundidad = `KeelOverLength` para el detector de tendencias.
   * Profundidad = `SlalomLength` para señales de entrada.
3. Realice un seguimiento del pivote KeelOver más reciente para determinar si la tendencia es alcista (el último pivote es bajo)
o hacia abajo (el último pivote es alto).
4. Cuando el indicador Slalom publique un nuevo pivote, arme una ruptura en esa dirección.
5. Calcule el precio ponderado `(5×Close + 2×Open + High + Low) / 9`. Si el precio se mueve más de
`BreakoutDistancePoints` (convertido en unidades de precio) lejos del pivote mientras la tendencia respalda
el movimiento, ejecutar una orden de mercado.
6. Cerrar las posiciones existentes cuando la tendencia global cambie o aparezca el pivote de Slalom opuesto y
`CloseOnOppositePivot` está habilitado.
7. Reinicie el contador de operaciones diario con cada cambio de día calendario.

Los parámetros `DeviationPoints` y `Backstep` se comparten entre ambas instancias de ZigZag, por lo que
La estructura swing coincide con los buffers del indicador MetaTrader.

## Parámetros
| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `CandleType` | `15m` | Plazo principal utilizado para construir ambas escaleras ZigZag. |
| `KeelOverLength` | `55` | Lookback en ZigZag de largo plazo que define la tendencia (original `KeelOver`). |
| `SlalomLength` | `17` | Retrospectiva en ZigZag a corto plazo utilizada para las entradas (original `Slalom`). |
| `DeviationPoints` | `5` | Tamaño mínimo del swing en puntos antes de que se confirme un nuevo pivote en ZigZag. |
| `Backstep` | `3` | Distancia de barra requerida entre pivotes consecutivos. |
| `BreakoutDistancePoints` | `2` | Distancia desde un pivote (en puntos) antes de disparar una orden. |
| `MaxTradesPerDay` | `1` | Número máximo de entradas por día natural. Refleja la bandera `newday` original. |
| `CloseOnOppositePivot` | `true` | Cerrar posiciones abiertas cuando el Slalom ZigZag produzca el swing contrario. |

Todos los parámetros basados en puntos se convierten a unidades de precio usando `Security.PriceStep`. Si el instrumento
no tiene ningún paso de precio configurado, se utiliza un valor de `1` para mantener la estrategia funcional durante la prueba.

## Notas de uso
* La estrategia opera con órdenes de mercado (`BuyMarket` / `SellMarket`). Adjunte sus propias reglas de riesgo
o ayudantes de stop-loss si se requiere una gestión de riesgos más estricta.
* Debido a que ambos indicadores ZigZag comparten el mismo flujo de velas, asegúrese de que el `CandleType` elegido sea
compatible con su adaptador de datos.
* `MaxTradesPerDay = 1` reproduce el comportamiento de "una operación por día". Aumente el valor si lo necesita
múltiples entradas durante la misma sesión.
* Configure `CloseOnOppositePivot = false` para mantener las posiciones abiertas hasta que la tendencia global se revierta en lugar de
reaccionando a cada cambio a corto plazo.

## Diferencias vs. el asesor experto MT4
* La versión MetaTrader colocó flechas de límite pendiente. El puerto StockSharp ejecuta rupturas con
órdenes de mercado inmediatas para permanecer dentro del nivel alto API.
* Se omiten intencionalmente la gestión de riesgos, el tamaño de los lotes y los cierres parciales. Utilice la posición StockSharp
Dimensionamiento de ayudantes si necesita un control de capital avanzado.
* Los buffers de indicadores 4/5/6 se reemplazan por lógica de estrategia directa y anotaciones de gráficos a través de
`DrawIndicator` y `DrawOwnTrades`.

## Extensiones recomendadas
* Agregue parámetros de stop-loss y take-profit vinculados a ATR o cambios recientes en ZigZag.
* Superponga el indicador original con `BreakoutDistancePoints = 0` para visualizar la escalera de pivote sin formato.
* Combínelo con un filtro de sesión (`IsFormedAndOnlineAndAllowTrading`) para limitar el horario comercial.
