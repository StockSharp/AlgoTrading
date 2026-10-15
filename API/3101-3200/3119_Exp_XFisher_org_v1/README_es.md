# Estrategia Exp XFisher org v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia reproduce el experto de MetaTrader 5 **Exp_XFisher_org_v1**. Opera reversiones detectadas en la transformada de Fisher del precio que además se suaviza con una media móvil configurable. El puerto de StockSharp conserva la naturaleza contratendencia del robot original: cuando la curva de Fisher gira hacia abajo después de una subida se abre una posición larga, y cuando la curva gira hacia arriba después de una bajada se abre una posición corta. Las posiciones existentes se cierran una vez que el indicador revierte en la dirección opuesta.

El indicador auxiliar `XFisherOrgIndicator` implementado en `CS/ExpXFisherOrgV1Strategy.cs` sigue la lógica de MT5:

1. Tomar el máximo más alto y el mínimo más bajo durante `Length` velas completadas.
2. Convertir la fuente de precio seleccionada (ver *Applied Price* a continuación) al rango 0–1 usando esos extremos.
3. Aplicar el filtro recursivo `value = (wpr - 0.5) + 0.67 * value[prev]` seguido de la transformada de Fisher
   `fish = 0.5 * ln((1 + value) / (1 - value)) + 0.5 * fish[prev]`.
4. Suavizar el resultado con una de las medias móviles compatibles. El valor de Fisher suavizado forma la línea principal; la línea de señal es simplemente el valor de la barra anterior, exactamente como en la versión MQL donde el búfer #1 almacena un desplazamiento de una barra.

La conversión mantiene los valores predeterminados originales (`Length = 7`, suavizado Jurik de longitud 5, fase 15, velas H4) y expone los mismos interruptores de habilitación/deshabilitación para abrir y cerrar operaciones largas/cortas.

## Reglas de trading
- **Entrada larga** – cuando el valor de Fisher de `SignalBar + 1` barras atrás estaba subiendo (`Fisher[SignalBar+1] > Fisher[SignalBar+2]`)
  pero el valor en `SignalBar` cruza por debajo o toca su copia retrasada (`Fisher[SignalBar] <= Fisher[SignalBar+1]`).
- **Entrada corta** – cuando el valor de Fisher de `SignalBar + 1` barras atrás estaba bajando pero el valor en `SignalBar` cruza por encima
  de su copia retrasada.
- **Salida de posición** – la reversión opuesta cierra una posición existente antes de considerar una nueva operación. Una salida larga se activa
  por la misma condición que abre un corto, y viceversa.
- **Volumen** – controlado por `OrderVolume`. Cuando se requiere un giro de corto a largo (o de largo a corto) la estrategia envía
  una única orden de mercado con suficiente volumen para cerrar la posición anterior y abrir la nueva en la misma transacción.

Todos los cálculos usan **únicamente velas completadas**. Si `SignalBar` es cero se usa la vela cerrada actual para la evaluación de señales;
los valores positivos desplazan la señal hacia atrás en el tiempo exactamente como el input `SignalBar` de MT5.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `OrderVolume` | Volumen de cada orden de mercado. | `1` |
| `BuyOpenAllowed` / `SellOpenAllowed` | Permitir abrir operaciones largas/cortas. | `true` |
| `BuyCloseAllowed` / `SellCloseAllowed` | Permitir cerrar operaciones largas/cortas existentes. | `true` |
| `SignalBar` | Desplazamiento (en velas cerradas) usado para leer los búferes de Fisher. | `1` |
| `Length` | Lookback para los extremos de precio más altos/bajos. | `7` |
| `SmoothingLength` | Período de la media de suavizado. | `5` |
| `Phase` | Fase Jurik (ignorada por otros métodos). | `15` |
| `SmoothingMethod` | Media móvil aplicada a la salida de Fisher. | `Jjma` |
| `PriceType` | Applied price enviada al indicador (cierre, apertura, mediana, etc.). | `Close` |
| `CandleType` | Serie de velas usada para el cálculo (predeterminado: velas de 4 horas). | `H4` |

## Mapeo del método de suavizado
El indicador original expone un gran conjunto de kernels de suavizado. El puerto de StockSharp los mapea a implementaciones incorporadas confiables:

- `Jjma`, `Jurx`, `T3` → `JurikMovingAverage` (parámetro de fase aplicado cuando la propiedad está disponible).
- `Sma`, `Ema`, `Smma`, `Lwma` → respectivas medias móviles de StockSharp.
- `Parabolic` → aproximado por `ExponentialMovingAverage` (comportamiento más cercano en StockSharp).
- `Vidya`, `Ama` → `KaufmanAdaptiveMovingAverage` (el comportamiento adaptativo VIDYA se modela con Kaufman AMA).

Este mapeo refleja el enfoque usado en otras conversiones de Kositsin en el repositorio y mantiene la respuesta de la línea de Fisher suavizada comparable a la implementación de MT5.

## Diferencias con el experto de MT5
- **Gestión del dinero** – las estrategias de StockSharp operan en volúmenes explícitos. Los inputs `MM`/`MarginMode` de MT5 se reemplazan con un único parámetro `OrderVolume` para que el trader pueda definir el tamaño del lote directamente.
- **Modelo de ejecución** – las operaciones se generan una vez por vela completada a través de la API de suscripción de alto nivel en lugar de en cada tick. Esto evita órdenes duplicadas y elimina la necesidad del helper `IsNewBar` original.
- **Opciones de applied price** – todos los modos de precio de `SmoothAlgorithms.mqh` son compatibles, incluidas las variantes TrendFollow y Demark.
- **Charting** – la estrategia dibuja velas, la transformada de Fisher suavizada y las operaciones ejecutadas en el área de gráfico predeterminada.

## Archivos
- `CS/ExpXFisherOrgV1Strategy.cs` – clase de estrategia, implementación del indicador y contenedor de valores.
