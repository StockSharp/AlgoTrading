# Estrategia Maybeawo222
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Maybeawo222 replica el asesor experto MetaTrader "maybeawo222" utilizando el API de alto nivel de StockSharp. Opera con un solo instrumento con un cruce de promedio móvil simple (SMA) en la vela anterior y limita la actividad a una ventana de tiempo configurable. La conversión mantiene la gestión del equilibrio por etapas que intenta asegurar las ganancias tan pronto como el precio avanza distancias predefinidas.

## Lógica de trading
1. La estrategia se suscribe a la serie de velas principales seleccionada a través de `CandleType` y calcula una media móvil simple con el período especificado por `MovingPeriod`.
2. Al cierre de cada vela, el valor SMA se desplaza `MovingShift` barras antes de usarse en la decisión. Esto reproduce la llamada `iMA` original con un parámetro de cambio.
3. Las señales comerciales solo se evalúan cuando la hora de cierre de la vela terminada cae dentro del rango `[StartHour, EndHour)`. Fuera de esa ventana no se crean nuevas órdenes, aunque se siguen gestionando posiciones abiertas.
4. Aparece una señal de **compra** cuando la vela anterior (la que acaba de cerrar) se abre por debajo del SMA desplazado y cierra por encima de él. Una señal de **venta** requiere el cruce opuesto. La estrategia invierte las posiciones existentes si es necesario, de modo que sólo queda abierta una dirección.
5. En cada vela terminada, el motor comprueba los extremos alto/bajo para detectar paradas de pérdida o toma de ganancias. Cada vez que se toca cualquiera de los niveles, se activa inmediatamente la salida del mercado correspondiente.
6. La posición también activa hasta dos ajustes de equilibrio por etapas. Una vez que la ganancia flotante supera `BreakevenPips1`, el stop se acerca a la entrada según `DesiredBreakevenDistancePips1`. Una segunda etapa repite el proceso con `BreakevenPips2` y `DesiredBreakevenDistancePips2`.

## Gestión del riesgo
- Las distancias iniciales de stop-loss y take-profit se configuran en pips. La conversión utiliza el instrumento `PriceStep` y aplica el factor convencional MetaTrader de 10 para cotizaciones de tres y cinco dígitos.
- Los niveles de equilibrio solo se aplican una vez por lado de la posición. Cada nueva entrada restablece las banderas, lo que permite que la parada se realice dos veces durante la vida de la operación.
- Las salidas de posición utilizan órdenes de mercado para que el motor pueda cerrar operaciones incluso si los niveles de parada o objetivo no están disponibles por parte del corredor.

## Parámetros
| Nombre | Predeterminado | Rango / Notas | Descripción |
|------|---------|---------------|-------------|
| `MovingPeriod` | `14` | Entero positivo | SMA longitud utilizada para la verificación de cruce. |
| `MovingShift` | `0` | `0` – `10` (sugerido) | Número de velas completadas para desplazar el valor SMA hacia atrás. |
| `StopLossPips` | `100` | `0` desactiva | Distancia desde el precio de entrada hasta el stop-loss protector, medida en pips. |
| `TakeProfitPips` | `800` | `0` desactiva | Distancia desde la entrada hasta el nivel de toma de ganancias, medida en pips. |
| `BreakevenPips1` | `180` | `0` desactiva | Umbral de beneficio (en pips) que desencadena el primer ajuste de equilibrio. |
| `DesiredBreakevenDistancePips1` | `60` | Cualquier no negativo | Nueva distancia de parada desde la entrada después de los incendios de la etapa 1 de equilibrio. |
| `BreakevenPips2` | `500` | `0` desactiva | Umbral de beneficio (en pips) que desencadena el segundo ajuste de equilibrio. |
| `DesiredBreakevenDistancePips2` | `350` | Cualquier no negativo | Nueva distancia de parada desde la entrada después de los incendios de la etapa de equilibrio 2. |
| `StartHour` | `3` | `0` – `23` | Hora de inicio de la sesión de negociación incluida, basada en la hora de cambio. |
| `EndHour` | `22` | `0` – `23` | Hora exclusiva de finalización de la sesión bursátil. |
| `OrderVolume` | `0.5` | Mayor que `0` | Volumen enviado con cada orden de mercado antes de la compensación de posiciones. |
| `CandleType` | `H1` | Cualquier tipo de datos de vela | Serie de velas utilizada para generar señales y calcular el SMA. |

## Notas de uso
- Asegúrese de que la seguridad conectada proporcione un `PriceStep` válido; de lo contrario, la conversión de pips vuelve a `1`. Ajuste los parámetros relacionados con los pips en consecuencia si su instrumento cotiza en ticks grandes.
- La estrategia espera una configuración de un solo símbolo. Agréguelo a un esquema con el instrumento deseado antes de iniciar la estrategia.
- Para el comercio en vivo, considere habilitar asignaciones de deslizamiento u órdenes de parada protectoras a través de extensiones específicas del corredor si las salidas del mercado no son suficientes.
