# Estrategia Bollinger Bands N Posiciones v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el expert advisor "Bollinger Bands N positions v2" de Vladimir Karputov. Opera en velas completadas y busca rompimientos de precio relativos al envelope de Bollinger Bands. El port a StockSharp mantiene el comportamiento de piramidación original, controles de riesgo y lógica de trailing mientras adapta la gestión de órdenes al modelo de compensación de la plataforma.

## Lógica de trading
- Un indicador de Bollinger Bands (período y desviación configurables) se calcula en la serie de velas seleccionada.
- Cuando el cierre de la vela termina por encima de la banda superior, la estrategia cierra cualquier exposición corta activa y abre una posición larga adicional (hasta el máximo número configurado de entradas apiladas).
- Cuando el cierre de la vela termina por debajo de la banda inferior, la estrategia cierra cualquier exposición larga activa y abre una posición corta adicional (también limitada por el parámetro de entradas máximas).
- El tamaño de posición se incrementa en incrementos fijos (el parámetro **Volume**) cuando se piramida en la misma dirección.
- El precio de entrada promedio de la posición apilada se rastrea para gestionar los niveles de stop loss, take profit y trailing stop de forma consistente.

## Gestión de riesgos
- Las distancias de stop loss y take profit se ingresan en pips. Se convierten en desplazamientos de precio absolutos multiplicando con el paso de precio del instrumento. Los instrumentos cotizados con 3 o 5 decimales multiplican automáticamente el paso por 10 para emular el ajuste de tamaño de pip de MetaTrader.
- El offset del trailing stop y el paso del trailing también se configuran en pips. El mecanismo de trailing actualiza el precio del stop solo después de que la operación se mueve `TrailingStop + TrailingStep` pips desde la entrada promedio actual. Cada actualización desplaza el stop por el offset del trailing mientras respeta el buffer de paso extra para evitar modificaciones excesivas.
- Las órdenes de salida protectoras se simulan dentro de la estrategia: siempre que una vela terminada cruce el nivel de stop o objetivo, la posición completa se cierra usando órdenes de mercado.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| **Bollinger Period** | Período de retroceso para la media móvil de Bollinger Bands. |
| **Bollinger Deviation** | Multiplicador de desviación estándar para el envelope de Bollinger. |
| **Max Positions** | Número máximo de entradas apiladas permitidas por dirección. |
| **Volume** | Volumen de orden para cada entrada individual. |
| **Stop Loss (pips)** | Distancia de stop loss en pips (0 deshabilita el stop). |
| **Take Profit (pips)** | Distancia de take profit en pips (0 deshabilita el objetivo). |
| **Trailing Stop (pips)** | Distancia del trailing stop en pips (0 deshabilita el trailing). |
| **Trailing Step (pips)** | Beneficio adicional en pips requerido antes de mover el trailing stop nuevamente. Debe ser positivo cuando el trailing está habilitado. |
| **Candle Type** | Serie de velas procesada por la estrategia. |

## Notas de implementación
- La estrategia usa suscripciones de velas de alto nivel con vinculación de indicadores, siguiendo las pautas de StockSharp.
- Solo se procesan las velas terminadas para replicar la lógica original de "nueva barra" de MetaTrader.
- Dado que StockSharp opera en modo de compensación, la conversión cierra la exposición opuesta antes de abrir una nueva capa de pirámide en la otra dirección.
- El paso del trailing debe mantenerse mayor que cero siempre que el trailing stop esté activo, coincidiendo con la verificación de seguridad del expert advisor original.
- La implementación en Python no está incluida en esta versión.
