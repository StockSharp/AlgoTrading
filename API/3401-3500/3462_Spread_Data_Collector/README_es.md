# Estrategia de recopilación de datos extendidos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de recopilador de datos extendido** es un puerto StockSharp de la utilidad MetaTrader 5 "Recolector de datos extendido" (MQL entrada 33314). El asesor experto original no realiza pedidos; en cambio, escucha el flujo de oferta/demanda y cuenta cuántos ticks caen dentro de rangos de diferenciales predefinidos. Cada vez que cambia el año comercial o el experto se detiene, imprime un resumen estadístico. Esta versión de C# reproduce el mismo comportamiento utilizando `SubscribeLevel1()` API de alto nivel y expone los umbrales de rango como parámetros configurables.

## Detalles de la operación
- La estrategia se suscribe a las actualizaciones de Nivel 1 (oferta/demanda) del `Security` principal cuando se inicia.
- Cada vez que los precios de oferta y demanda están disponibles, la estrategia calcula el diferencial y lo convierte en unidades de precio multiplicando los límites de puntos configurados por `Security.PriceStep`.
- Se mantienen seis contadores:
  1. Difundir estrictamente por debajo del primer umbral.
  2. Distribución entre el primer y segundo umbral.
  3. Distribución entre el segundo y tercer umbral.
  4. Distribución entre el tercer y cuarto umbral.
  5. Distribución entre el cuarto y quinto umbral.
  6. Difundir por encima del quinto umbral.
- Las transiciones de año se detectan a partir de la marca de tiempo del intercambio (`Level1ChangeMessage.ServerTime`). Cuando cambia el año, la estrategia imprime el resumen del año terminado y reinicia los contadores.
- Cuando la estrategia se detiene, imprime las estadísticas del año en curso antes de cerrarse.

El puerto mantiene la naturaleza de solo registro de la utilidad MQL, lo que permite a los operadores analizar cómo se comportaron los diferenciales durante diferentes períodos sin enviar órdenes ni manipular posiciones.

## Parámetros
Todas las entradas se expresan en **puntos** (terminología MetaTrader). La distancia del precio real se calcula como `points × Security.PriceStep`.

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `FirstBucketPoints` | 10 | Límite superior del primer cubo esparcido. Los diferenciales estrictamente por debajo de este límite se cuentan en la primera categoría. |
| `SecondBucketPoints` | 20 | Límite superior del segundo cubo de dispersión. Aquí se cuentan los diferenciales en `[FirstBucketPoints, SecondBucketPoints)`. |
| `ThirdBucketPoints` | 30 | Límite superior del tercer cubo de dispersión. Los diferenciales en `[SecondBucketPoints, ThirdBucketPoints)` aumentan este contador. |
| `FourthBucketPoints` | 40 | Límite superior del cuarto cubo de dispersión. Aquí se registran los diferenciales en `[ThirdBucketPoints, FourthBucketPoints)`. |
| `FifthBucketPoints` | 50 | Límite superior del quinto cubo de dispersión. Los diferenciales en `[FourthBucketPoints, FifthBucketPoints)` aumentan este contador. |

Todos los umbrales deben ser estrictamente crecientes. Intentar iniciar la estrategia con valores `Security.PriceStep` no válidos o no positivos da como resultado una excepción de tiempo de ejecución, que protege al usuario de estadísticas inconsistentes.

## Registros y salidas
Las estadísticas se imprimen a través de `AddInfoLog` en el siguiente formato:

```
Año=2024 Spread<=10pts=15342 Spread_10_20pts=2841 Spread_20_30pts=912... Spread>50pts=37
```

Este resultado refleja las declaraciones `Print` del experto MetaTrader, lo que facilita la comparación de ambos entornos. Utilice el visor de registros StockSharp o redirija los registros a un archivo para realizar un análisis más detallado.

## Lista de verificación de uso
1. Asigne el instrumento de destino a `Strategy.Security` y asegúrese de que su `PriceStep` coincida con el tamaño en puntos MetaTrader (para la mayoría de los símbolos Forex, esto equivale a 0,0001).
2. Ajuste los umbrales del cucharón si necesita diferentes rangos de dispersión. Mantenga los valores estrictamente ascendentes.
3. Inicie la estrategia y déjela funcionar. No se enviarán pedidos.
4. Revise los registros anuales para comprender el comportamiento de distribución entre sesiones.

La estrategia es intencionalmente liviana y segura para ejecutarla junto con sistemas comerciales reales. Ayuda a las mesas a construir distribuciones históricas de diferenciales, validar supuestos de liquidez y monitorear las condiciones de los corredores durante largos períodos.
