# Estrategia Exp Skyscraper Fix Color AML MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Exp Skyscraper Fix Color AML MMRec es el port de StockSharp del asesor experto MQL5 *Exp_Skyscraper_Fix_ColorAML_MMRec*. El robot original combina dos indicadores independientes — **Skyscraper Fix** y **Color AML** — y aplica la lógica de gestión monetaria MMRec para reducir el tamaño de la orden después de pérdidas consecutivas. La implementación en C# mantiene ambas fuentes de señales y el dimensionamiento adaptativo de posiciones mientras usa la API de alto nivel de StockSharp para el enrutamiento de órdenes.

## Flujo de trading

1. **Módulo Skyscraper Fix** construye un canal adaptativo a partir de las velas terminadas de `SkyscraperCandleType`. Cuando el color del canal se vuelve teal (tendencia &gt; 0), cada posición corta puede cerrarse y, si el color anterior no era teal, se abre una nueva operación larga. Cuando el color se vuelve rojo (tendencia &lt; 0), la lógica se refleja para operaciones cortas. La clase auxiliar `SkyscraperFixIndicator` se reutiliza de la estrategia `3040_Exp_Skyscraper_Fix_Duplex`.
2. **Módulo Color AML** procesa las velas de `ColorAmlCandleType`. El `ColorAmlIndicator` traducido reproduce el nivel de mercado adaptativo y emite un código de color: `2` (alcista), `0` (bajista) o `1` (neutral). El módulo cierra el lado opuesto siempre que se detecta un color alcista o bajista y abre una nueva posición si el color cambió respecto a la muestra retrasada anterior.
3. **Retraso de señal** se controla independientemente para ambos módulos a través de `SkyscraperSignalBar` y `ColorAmlSignalBar`. La estrategia mantiene colas de salidas de indicadores y ejecuta órdenes solo después del número configurado de velas cerradas, coincidiendo con el comportamiento `CopyBuffer(..., shift, ...)` en el asesor experto.
4. **Gestión de riesgo** refleja las distancias originales de stop/take-profit. Cada módulo define sus propias distancias protectoras en pasos de precio (ticks). La estrategia las traduce a precios absolutos y, en cada vela terminada, verifica si el rango de la barra tocó un stop-loss o take-profit. Si es así, la posición se nivela con una orden de mercado y todos los niveles protectores se borran.
5. **Gestión monetaria MMRec** rastrea las pérdidas consecutivas por separado para los longs Skyscraper, shorts Skyscraper, longs Color AML y shorts Color AML. Cuando la racha de pérdidas para una dirección alcanza el disparador correspondiente (`*LossTrigger`), el volumen cambia de `*Mm` al valor reducido `*SmallMm`. Una vez que aparece una operación rentable, la racha se restablece a cero. Dado que la estrategia de muestra se ejecuta en una posición neta única, solo el modo de gestión `Lot` tiene efecto práctico; otros modos recurren al dimensionamiento directo de lotes.

## Notas de implementación

- El código depende exclusivamente de la API de alto nivel de StockSharp: las suscripciones de velas alimentan ambos indicadores y todas las decisiones de trading se ejecutan a través de los ayudantes `BuyMarket`, `SellMarket` y `ClosePosition`.
- Las órdenes protectoras se implementan con salidas de mercado en lugar de órdenes stop/limit separadas. Esto evita conflictos cuando ambos módulos comparten la misma posición neta.
- La gestión monetaria usa los datos de ejecución recibidos en `OnOwnTradeReceived` para determinar el resultado de la operación anterior. El módulo que abrió la posición almacena su identificador para que el contador de pérdidas correcto se actualice cuando la posición se cierra.
- El `ColorAmlIndicator` traducido almacena velas y valores de suavizado en caché para seguir el esquema de suavizado exponencial original, incluido el alpha dinámico basado en rangos fractales y la lógica de codificación de colores (azul para AML creciente, rojo para caída, gris en caso contrario).
- Los números mágicos y configuraciones explícitas de deslizamiento de la versión MQL5 no son necesarios en StockSharp y, por lo tanto, se omiten.

## Parámetros

### Módulo Skyscraper Fix

| Parámetro | Por defecto | Descripción |
| --- | --- | --- |
| `SkyscraperCandleType` | Velas H4 | Marco temporal usado para calcular el canal Skyscraper Fix. |
| `SkyscraperLength` | 10 | Lookback de ATR usado para definir el paso del canal adaptativo. |
| `SkyscraperKv` | 0.9 | Multiplicador aplicado al tamaño de paso basado en ATR. |
| `SkyscraperPercentage` | 0 | Offset porcentual aplicado a la línea media. |
| `SkyscraperMode` | HighLow | Fuente de precio para el envelope (high/low o close). |
| `SkyscraperSignalBar` | 1 | Número de velas cerradas para retrasar las señales de Skyscraper. |
| `SkyscraperEnableLongEntry` | true | Permitir entradas largas cuando el canal se vuelve alcista. |
| `SkyscraperEnableShortEntry` | true | Permitir entradas cortas cuando el canal se vuelve bajista. |
| `SkyscraperEnableLongExit` | true | Cerrar posiciones largas en señales bajistas de Skyscraper. |
| `SkyscraperEnableShortExit` | true | Cerrar posiciones cortas en señales alcistas de Skyscraper. |
| `SkyscraperBuyLossTrigger` | 2 | Pérdidas largas consecutivas necesarias para cambiar al volumen reducido. |
| `SkyscraperSellLossTrigger` | 2 | Pérdidas cortas consecutivas necesarias para cambiar al volumen reducido. |
| `SkyscraperSmallMm` | 0.01 | Volumen de orden usado tras alcanzar el disparador de pérdidas. |
| `SkyscraperMm` | 0.1 | Volumen de orden predeterminado para señales de Skyscraper. |
| `SkyscraperMmMode` | Lot | Modo de gestión monetaria (solo `Lot` afecta al port en C#). |
| `SkyscraperStopLossTicks` | 1000 | Distancia de stop-loss en pasos de precio. Un valor de 0 desactiva el stop. |
| `SkyscraperTakeProfitTicks` | 2000 | Distancia de take-profit en pasos de precio. Un valor de 0 desactiva el objetivo. |

### Módulo Color AML

| Parámetro | Por defecto | Descripción |
| --- | --- | --- |
| `ColorAmlCandleType` | Velas H4 | Marco temporal usado por el indicador Color AML. |
| `ColorAmlFractal` | 6 | Ventana fractal para los cálculos de rango AML. |
| `ColorAmlLag` | 7 | Lag de suavizado para el promedio exponencial AML. |
| `ColorAmlSignalBar` | 1 | Número de velas cerradas para retrasar señales de Color AML. |
| `ColorAmlEnableLongEntry` | true | Permitir entradas largas cuando AML se vuelve alcista (color 2). |
| `ColorAmlEnableShortEntry` | true | Permitir entradas cortas cuando AML se vuelve bajista (color 0). |
| `ColorAmlEnableLongExit` | true | Cerrar posiciones largas en colores AML bajistas. |
| `ColorAmlEnableShortExit` | true | Cerrar posiciones cortas en colores AML alcistas. |
| `ColorAmlBuyLossTrigger` | 2 | Pérdidas largas consecutivas antes de cambiar al volumen reducido. |
| `ColorAmlSellLossTrigger` | 2 | Pérdidas cortas consecutivas antes de cambiar al volumen reducido. |
| `ColorAmlSmallMm` | 0.01 | Volumen de orden usado tras alcanzar el disparador de pérdidas. |
| `ColorAmlMm` | 0.1 | Volumen de orden predeterminado para señales de Color AML. |
| `ColorAmlMmMode` | Lot | Modo de gestión monetaria (solo `Lot` afecta al port en C#). |
| `ColorAmlStopLossTicks` | 1000 | Distancia de stop-loss en pasos de precio. Establecer en 0 para deshabilitar. |
| `ColorAmlTakeProfitTicks` | 2000 | Distancia de take-profit en pasos de precio. Establecer en 0 para deshabilitar. |

## Uso

1. Adjunte la estrategia a un portafolio y al instrumento que desea operar. El instrumento debe proporcionar las series de velas definidas por `SkyscraperCandleType` y `ColorAmlCandleType`.
2. Ajuste los parámetros de gestión monetaria si su broker usa un paso de lote diferente. Solo se aplica el dimensionamiento directo de lotes, así que configure `*Mm` y `*SmallMm` en consecuencia.
3. Opcionalmente modifique las distancias de stop-loss y take-profit (en ticks) para cada módulo. Establecer una distancia en cero desactiva la protección correspondiente.
4. Inicie la estrategia. Se suscribirá a ambas transmisiones de velas, calculará los indicadores y gestionará entradas y salidas automáticamente según las reglas anteriores.

El README refleja el comportamiento de `CS/ExpSkyscraperFixColorAmlMmrecStrategy.cs` y debe usarse como documentación de referencia para esta implementación de StockSharp.
