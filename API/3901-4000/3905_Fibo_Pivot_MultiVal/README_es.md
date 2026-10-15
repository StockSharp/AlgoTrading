# Estrategia Fibo Pivot MultiVal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia Fibo Pivot MultiVal** es una StockSharp versión del MetaTrader 4 asesor experto `_Fibo_Pivot_multiVal.mq4`. el
La estrategia combina puntos de pivote diarios con Fibonacci ratios de retroceso y extensión para implementar órdenes límite dentro de cada precio.
zona que rodea el pivote. Las sesiones de negociación, los objetivos de posición y las reglas de suspensión siguen al asesor experto original para que
El control de riesgos y el comportamiento de ejecución siguen siendo familiares para los comerciantes que utilizaron la versión MetaTrader.

## Lógica principal

1. **Los niveles de referencia diarios** se calculan a partir del máximo, el mínimo y el cierre del día anterior. Niveles de pivote clásicos (P, R1-R3, S1-S3)
van acompañados de niveles internos basados en Fibonacci que dividen la distancia entre el pivote y el soporte vecino o
líneas de resistencia. Las extensiones adicionales de R3/S3 proyectan posibles objetivos de ruptura.
2. La **acción del precio intradía** se monitorea en el período de tiempo de vela configurado (15 minutos de forma predeterminada). Cuando el cierre actual
reside dentro de una zona de pivote particular (por ejemplo, entre R2 y R3), la estrategia activa las órdenes límite correspondientes.
3. **Los pedidos limitados** se realizan en los subniveles Fibonacci. Cada zona mantiene órdenes tanto largas como cortas, con la dirección
filtrado por el parámetro `MidZoneOrderMode` cuando el precio oscila entre R1-R2 y S1-S2.
4. **Los objetivos** se adaptan a la volatilidad del mercado. Cuando `UseReversalTargets` está habilitado, las salidas se encuentran en el lado opuesto del activo
Fibonacci banda para capturar rebotes de reversión a la media. Cuando está deshabilitado, el algoritmo compara el rango del día anterior con el
Umbrales `LimitPointOut` y `LimitPointIn` para decidir si apuntar a rupturas extendidas (hacia extensiones R3/S3) o
reversiones más profundas (hacia el pivote).
5. **Límites de riesgo** pausa las nuevas operaciones una vez que se exceden los umbrales configurables de ganancias/operaciones diarias o por símbolo. Todo pendiente
las órdenes se cancelan y las operaciones se reanudan en el siguiente reinicio de sesión (antes de `StartTime`).
6. **La gestión de sesiones** refleja la EA original: las operaciones comienzan en `StartTime`, las nuevas entradas terminan después de `FinishTime` y todas
la exposición abierta se aplana después de `CloseAllTime`.

## Parámetros

| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `CandleType` | velas de 15 minutos | Marco de tiempo utilizado para construir las velas de decisión. |
| `OrderVolume` | `0.1` | Volumen por cada orden límite registrada por la estrategia. |
| `StartTime` | `00:01` | Hora de sesión del día que permite operar y restablecer contadores. |
| `FinishTime` | `08:00` | Tiempo de sesión que deshabilita nuevas entradas manteniendo las posiciones existentes. |
| `CloseAllTime` | `12:00` | Tiempo de sesión que cancela órdenes y cierra todas las posiciones. |
| `UseReversalTargets` | `true` | Cuando es verdadero, los objetivos permanecen dentro de la zona Fibonacci. Cuando es falso, se utilizan objetivos de ruptura/pivote en función del rango diario. |
| `LimitPointIn` | `150` | Umbral de rango diario (puntos) que impone objetivos de reversión de pivote cuando se excede. |
| `LimitPointOut` | `50` | Umbral de rango diario (puntos) que fomenta objetivos de ruptura cuando se comprime la acción del precio. |
| `LevelPf1` | `33` | Porcentaje utilizado para dividir la distancia Pivot-R1 y Pivot-S1. |
| `LevelF1F2` | `50` | Porcentaje utilizado para calcular el nivel intermedio entre R1–R2 y S1–S2. |
| `LevelF2F3` | `33` | Porcentaje utilizado para calcular el nivel intermedio entre R2–R3 y S2–S3. |
| `LevelF3Out` | `40` | Porcentaje utilizado para ampliar R3/S3 para objetivos de ruptura. |
| `MidZoneOrderMode` | `"bs"` | Direcciones permitidas dentro de las zonas medias (`"b"`=solo comprar, `"s"`=solo vender, `"bs"`=ambos). |
| `DailyProfitTarget` | `50` | Límite de ganancia diaria en puntos. |
| `DailyTradeTarget` | `35` | Número máximo de operaciones completadas por día. |
| `SymbolProfitTarget` | `150` | Objetivo de beneficio por símbolo en puntos. |
| `SymbolTradeTarget` | `15` | Operaciones máximas completadas por símbolo por día. |

## Gestión de órdenes

* Cada zona activa mantiene sus propias órdenes de entrada, toma de ganancias y stop opcionales. Cuando se completa una entrada, las órdenes de salida se
recreado utilizando los niveles de destino/parada derivados de la configuración Fibonacci.
* Las salidas completas actualizan las estadísticas diarias y por símbolo. Alcanzar cualquier límite detiene el comercio hasta el próximo reinicio.
* Los límites de la sesión cancelan automáticamente las órdenes de entrada. El límite `CloseAllTime` cierra adicionalmente cualquier posición abierta a través de
órdenes de mercado.

## Consejos prácticos

* La estrategia espera instrumentos con escalones de precios bien definidos. Asegúrese de que la instancia `Security` exponga `PriceStep` para que el
La conversión de punto a precio coincide con el EA original.
* Para activos con diferentes características de volatilidad, ajuste `LimitPointIn` y `LimitPointOut` para que la ruptura vs.
los comportamientos de reversión a la media se activan en rangos apropiados.
* Si prefiere operaciones direccionales alrededor de la zona media (R1-R2 o S1-S2), configure `MidZoneOrderMode` en `"b"` o `"s"` para permitir solo
configuraciones largas o cortas.
* Utilice el soporte de optimización de parámetros integrado para realizar pruebas retrospectivas de proporciones Fibonacci alternativas. Todos los parámetros porcentuales y
Los umbrales exponen `SetCanOptimize` en el código fuente, lo que permite escaneos automatizados dentro de StockSharp Designer.

## Diferencias frente al Expert Advisor original

* La versión StockSharp funciona con una única seguridad por instancia de estrategia. Para intercambiar múltiples símbolos como en MetaTrader EA,
ejecute instancias de estrategia separadas para cada instrumento.
* El tamaño de la posición se expresa directamente en unidades de volumen en lugar de MetaTrader lotes. Configure `OrderVolume` para que coincida con su
Requisitos del corredor.
* La ejecución de la orden se basa en el nivel alto StockSharp API (`BuyLimit`, `SellLimit`, etc.). Comportamiento específico del corredor (como
compensaciones de pedidos pendientes) deben revisarse antes de implementarse en producción.
