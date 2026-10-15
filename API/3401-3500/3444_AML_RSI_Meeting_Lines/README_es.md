# Estrategia de líneas de encuentro AML RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de líneas de reunión AML RSI** es una StockSharp adaptación del MetaTrader asesor experto de 5 `Expert_AML_RSI.mq5`. El sistema original combina el reconocimiento de patrones de velas japonesas con el índice de fuerza relativa (RSI) para negociar reversiones de "líneas de encuentro" alcistas y bajistas. Esta conversión mantiene la lógica comercial central y la adapta al nivel alto de StockSharp API con suscripciones de velas e indicadores integrados.

## Lógica de trading
- Se suscribe a un tipo de vela configurable y procesa solo velas terminadas.
- Calcula una media móvil simple de los tamaños del cuerpo de las velas para detectar velas "largas" que forman patrones de líneas de encuentro.
- Realiza un seguimiento de los valores RSI en las dos velas completadas más recientes para señales de confirmación y salida.
- **Configuración alcista**: la reversión de las líneas de reunión de dos barras con RSI por debajo del umbral alcista desencadena entradas largas.
- **Configuración bajista**: el patrón reflejado con RSI por encima del umbral bajista desencadena entradas cortas.
- **Salidas de posición**: RSI cruces a través de niveles superior e inferior configurables cierran operaciones abiertas en la dirección opuesta.
- Utiliza ayudantes `BuyMarket`, `SellMarket` y `ClosePosition` para gestionar la exposición y cambia automáticamente el tamaño de la posición cuando aparece una señal contraria.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Marco de tiempo utilizado para evaluar los patrones de velas. | plazo de 1 hora |
| `RsiPeriod` | RSI longitud retrospectiva. | 11 |
| `BodyAveragePeriod` | Número de velas para el tamaño corporal medio. | 3 |
| `BullishRsiLevel` | Máximo RSI que valida Líneas de Reunión alcistas. | 40 |
| `BearishRsiLevel` | Mínimo RSI que valida Líneas de Encuentro bajistas. | 60 |
| `LowerExitLevel` | RSI nivel que cierra cortos en cruces alcistas. | 30 |
| `UpperExitLevel` | RSI nivel que cierra posiciones largas en cruces bajistas. | 70 |

Todos los parámetros están expuestos como objetos `StrategyParam<T>` para que puedan optimizarse en el diseñador StockSharp.

## Gestión del riesgo
- `StartProtection()` se invoca en `OnStarted` para habilitar el monitoreo de posición integrado del marco.
- La estrategia cierra la exposición existente cada vez que RSI cruza los límites de salida configurados antes de considerar nuevas señales.
- Las órdenes de mercado invierten automáticamente la posición sumando el valor absoluto de la exposición actual al volumen configurado.

## Notas de conversión
- El promedio de velas japonesas utiliza `SimpleMovingAverage` alimentado con cuerpos de velas absolutos, reflejando el `AvgBody` ayudante de la fuente MQL5.
- La confirmación RSI se basa en los valores de las dos velas anteriores, reproduciendo las comprobaciones `RSI(1)` y `RSI(2)` del experto original.
- Todos los comentarios en el código se reescribieron en inglés y la estructura sigue el requisito del repositorio de espacios de nombres con ámbito de archivo con sangría de tabulación.

## Uso
1. Adjunte la estrategia a un valor en StockSharp y seleccione el tipo de vela deseado.
2. Configure RSI y los umbrales de salida para que coincidan con la volatilidad del lugar de negociación o del instrumento.
3. Ejecute primero la estrategia en el comercio en papel para validar el reconocimiento de patrones antes de pasar al comercio en vivo o a la optimización.
4. Utilice los parámetros proporcionados durante la optimización para ajustar los niveles de RSI y la longitud corporal promedio para diferentes mercados.

## Descargo de responsabilidad
Esta estrategia se proporciona únicamente con fines educativos. Pruebe minuciosamente datos históricos y en entornos simulados antes de implementarlos en capital real.
