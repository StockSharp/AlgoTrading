# Estrategia de Swing con MA Serial (API/2782)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Convierte el asesor experto SerialMA de MetaTrader en una estrategia de alto nivel de StockSharp usando suscripciones de velas y un indicador de media móvil serial personalizado.
- Abre nuevas posiciones de swing cada vez que la media móvil serial cambia su dirección relativa al precio, opcionalmente invirtiendo la señal y limitando el número de swings concurrentes.
- Implementa las mismas distancias de stop-loss y take-profit protectoras medidas en puntos del instrumento, recalculadas en cada vela completada.

## Indicador de Media Móvil Serial
El EA original depende del indicador personalizado *SerialMA* que reconstruye su media móvil después de cada cruce de precio. El indicador portado replica este comportamiento:
1. Acumulando precios de cierre desde el cruce más reciente y calculando su media aritmética.
2. Rastreando la diferencia entre la media y el cierre actual para detectar un cambio de signo.
3. Restableciendo la ventana interna cuando cambia el signo, efectivamente reiniciando el promedio desde la barra de cruce y señalizando el evento para la estrategia.

Esta implementación expone el valor de la media móvil junto con una bandera booleana que indica que un cruce ocurrió en la barra anterior, permitiendo que la estrategia refleje la lógica MQL sin acceso manual al búfer.

## Lógica de trading
1. En cada vela completada la estrategia lee el valor de la media móvil serial y la bandera de cruce.
2. Cuando la vela anterior desencadenó un cruce:
   - Si el cierre anterior estaba por encima de la media móvil anterior, se genera una señal larga.
   - Si el cierre anterior estaba por debajo de la media móvil anterior, se genera una señal corta.
3. El parámetro **ReverseSignals** opcionalmente intercambia entradas largas y cortas.
4. El parámetro **OpenedMode** controla el apilamiento de posiciones:
   - **AllSwing** abre una nueva orden en cada señal, incluso si ya existe una posición en esa dirección.
   - **SingleSwing** solo abre una nueva orden cuando no existe exposición en esa dirección.
5. Antes de enviar una nueva orden, la estrategia siempre cierra la exposición existente en la dirección opuesta para mantener la lógica de swing consistente con el EA fuente.
6. Las distancias de stop-loss y take-profit se aplican en cada vela usando el paso de precio del instrumento, coincidiendo con los controles de riesgo basados en puntos del experto original.

## Parámetros
| Nombre | Descripción | Valor predeterminado |
| --- | --- | --- |
| `OpenedMode` | Permite apilar swings o mantener un solo swing por dirección. | `AllSwing` |
| `EnableBuy` | Habilita o deshabilita entradas largas. | `true` |
| `EnableSell` | Habilita o deshabilita entradas cortas. | `true` |
| `ReverseSignals` | Invierte la dirección de trading. | `false` |
| `TradeVolume` | Tamaño de orden (lotes) para cada nuevo swing. | `1` |
| `StopLossPoints` | Distancia de stop-loss en pasos de precio (puntos). Un valor de `0` deshabilita el stop. | `0` |
| `TakeProfitPoints` | Distancia de take-profit en pasos de precio (puntos). Un valor de `0` deshabilita el take profit. | `0` |
| `CandleType` | Tipo de datos de vela usado para los cálculos. | `Velas de 5 minutos` |

## Gestión de órdenes y protección
- Cuando está en largo, la estrategia verifica si el mínimo de la vela violó el nivel de stop-loss o si el máximo de la vela alcanzó el objetivo de ganancia y emite una orden de mercado para aplanar en consecuencia.
- Cuando está en corto, el máximo de la vela activa el stop-loss y el mínimo de la vela activa el objetivo de ganancia.
- Los niveles de protección se miden en unidades de `PriceStep`. Si el instrumento no proporciona un paso de precio, las verificaciones de protección permanecen inactivas, reflejando el comportamiento de información de tamaño de tick faltante.

## Notas de uso
- La implementación usa la API de alto nivel de StockSharp (`SubscribeCandles` + `BindEx`) y evita la gestión de búfer de bajo nivel.
- No se incluye versión en Python, según lo solicitado. Solo el port en C# reside en `CS/SerialMASwingStrategy.cs`.
- La estrategia está destinada para ejecución de estilo swing similar al EA original; habilitar ambas direcciones y mantener el modo predeterminado `AllSwing` se asemeja más al comportamiento MQL.
