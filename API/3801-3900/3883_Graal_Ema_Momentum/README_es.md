# Estrategia de impulso general EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión del MetaTrader 4 asesor experto **0Graal-CROSSmuvingi**. Negocia cambios de tendencia que ocurren cuando un promedio móvil exponencial rápido (EMA) en los precios de cierre cruza un EMA más lento calculado en los precios de apertura. Un oscilador de impulso confirma la dirección de ruptura y una toma de ganancias de distancia fija replica el modelo de ejecución original de MT4.

## Idea comercial

1. **Fast EMA al cerrar** rastrea la acción del precio más reciente.
2. **EMA lenta en apertura** se queda atrás y forma la línea de base de cruce.
3. **Oscilador de impulso (período 14)** mide con qué fuerza se acelera el precio alejándose del valor neutral (100). La estrategia solo opera cuando el impulso se desvía de 100 en más de un filtro configurable y continúa fortaleciéndose en la misma dirección.
4. **Take Profit** cierra operaciones después de una distancia predefinida medida en puntos del instrumento, reflejando el parámetro MT4 `TakeProfit`.

## Reglas de entrada

- **Configuración larga**
  - El EMA rápido cruza por encima del EMA lento en la vela finalizada actual, mientras que la barra anterior tenía el EMA rápido por debajo o igual al EMA lento.
  - El impulso (valor menos 100) es mayor que el umbral `MomentumFilter` y también mayor que la lectura de impulso de la barra anterior.
  - Las posiciones cortas existentes se cierran antes de abrir una nueva larga. El nuevo tamaño largo es igual al `Volume` configurado más cualquier cantidad necesaria para invertir una posición corta abierta.
- **Configuración corta**
  - El EMA rápido cruza por debajo del EMA lento mientras que la barra anterior tenía el EMA rápido por encima o igual al EMA lento.
  - El impulso (valor menos 100) está por debajo del umbral negativo `MomentumFilter` y es menor que la lectura de impulso de la barra anterior.
  - Las posiciones largas existentes se cierran antes de abrir una nueva venta corta. El nuevo tamaño corto equivale al `Volume` configurado más la cantidad necesaria para cubrir un largo abierto.

## Reglas de salida

- Las posiciones se cierran automáticamente cuando el precio alcanza el objetivo de obtención de beneficios calculado (`TakeProfitPoints * PriceStep`).
- Una nueva señal opuesta también invierte la posición inmediatamente porque el tamaño del pedido siempre incluye la cantidad de la posición actual.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `FastPeriod` | Duración del EMA sobre precios de cierre. | 13 |
| `SlowPeriod` | Duración del EMA sobre precios de apertura. | 34 |
| `MomentumPeriod` | Mirada retrospectiva del oscilador de momento. | 14 |
| `MomentumFilter` | Desviación mínima del impulso absoluto de 100 requerida para operar. | 0.1 |
| `TakeProfitPoints` | Distancia al objetivo de ganancias en puntos de precio (multiplicada por `PriceStep`). | 200 |
| `CandleType` | Tipo de datos de vela utilizado para los cálculos (período de tiempo de 15 minutos de forma predeterminada). | plazo de 15 minutos |
| `Volume` | Tamaño del pedido utilizado para nuevas entradas. El motor lo hereda de la clase base. | 1 |

## Notas de implementación

- Las señales se procesan únicamente en velas cerradas (`CandleStates.Finished`).
- La estrategia se suscribe al tipo de vela elegido con `SubscribeCandles` y vincula tanto EMA como los indicadores de impulso a través del nivel alto API.
- El EMA lenta se actualiza manualmente con precios de apertura dentro de la devolución de llamada de enlace para replicar el comportamiento de MT4 donde se usó `PRICE_OPEN`.
- La gestión de toma de ganancias observa los altibajos intrabar para emular la lógica de salida basada en puntos de MT4.
- `StartProtection()` está habilitado al inicio para protegerse contra posiciones abiertas inesperadas antes de que la estrategia comience a operar.
