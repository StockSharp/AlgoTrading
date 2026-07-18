# RSI Estrategia de promedios alineados con el comerciante
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
Esta estrategia reproduce el asesor experto "RSI trader" MetaTrader. Alinea dos filtros de tendencia (medias móviles de precios y medias RSI suavizadas) para entrar en la dirección de la tendencia dominante y salir cuando los filtros divergen (régimen lateral). El puerto StockSharp funciona en cualquier instrumento con soporte de datos de velas y por defecto utiliza velas horarias como en la descripción original.

## como funciona
1. Calcule RSI con el período especificado por **RSI Período** (predeterminado 14).
2. Suaviza la secuencia RSI con dos medias móviles simples: una corta (**Short RSI MA**) y una larga (**Long RSI MA**).
3. Precios de cierre suaves con dos promedios móviles: un MA simple corto (**MA de precio corto**) y un MA largo ponderado lineal (**MA de precio largo**).
4. Genera señales solo en velas terminadas:
   - **Largo**: ambos promedios cortos (precio y RSI) están por encima de sus contrapartes largos.
   - **Corto**: ambos promedios cortos están por debajo de sus contrapartes largas.
   - **Lateralmente**: los promedios no están de acuerdo (uno indica tendencia alcista y el otro tendencia bajista). Cuando esto ocurre, cualquier posición abierta se cierra.
5. Los pedidos se emiten con `BuyMarket` / `SellMarket`. Las posiciones opuestas se aplanan antes de entrar en una nueva dirección.

## Parámetros
| Nombre | Descripción | Predeterminado | Optimizable |
| --- | --- | --- | --- |
| `RSI Period` | RSI longitud del cálculo. | 14 | Sí (7…28, paso 1) |
| `Short Price MA` | Longitud de la media móvil simple corta del precio. | 9 | Sí (5…20, paso 1) |
| `Long Price MA` | Longitud de la media móvil ponderada lineal larga del precio. | 45 | Sí (30…90, paso 5) |
| `Short RSI MA` | Longitud del promedio de suavizado corto aplicado a RSI. | 9 | Sí (5…20, paso 1) |
| `Long RSI MA` | Longitud del promedio de suavizado largo aplicado a RSI. | 45 | Sí (30…90, paso 5) |
| `Candle Type` | Tipo de datos utilizado para velas. El valor predeterminado es un período de tiempo de 1 hora. | H1 | No |

## Notas
- El comercio sólo se realiza cuando se forman todos los indicadores.
- El EA original usaba lotes y configuraciones de deslizamiento. StockSharp utiliza la propiedad de estrategia `Volume` para el tamaño de la orden y deja la gestión del deslizamiento de ejecución al adaptador comercial.
- No se define ningún stop-loss ni take-profit incorporado; las salidas dependen de la detección lateral. Se puede agregar gestión de riesgos adicional externamente.
- Los gráficos dibujan precios y RSI promedios móviles cuando el servicio de gráficos está disponible.
