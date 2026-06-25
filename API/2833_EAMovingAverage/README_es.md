# Estrategia EA Moving Average
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Convertida del asesor experto de MetaTrader **"EA Moving Average"** (edición barabashkakvn).
- Utiliza cuatro medias móviles independientes para controlar las entradas y salidas largas y cortas.
- Diseñada para un único símbolo en modo netting. El tipo de vela predeterminado es el marco temporal de 15 minutos, pero se puede seleccionar cualquier tipo de vela regular.
- La estrategia abre como máximo una posición a la vez. Mientras una posición está activa, solo se evalúan las reglas de salida.

## Lógica de trading
### Entrada larga
1. La vela actual debe cerrar por encima de la media móvil *Buy Open* después de abrir por debajo de ella (cruce verdadero dentro de una sola barra).
2. `UseBuy` debe estar habilitado.
3. Si `ConsiderPriceLastOut` está habilitado, el precio actual debe ser menor o igual al precio de la última operación cerrada. Esto evita comprar por encima de la salida más reciente.
4. Cuando se cumplen las condiciones, la estrategia envía una orden de compra a mercado dimensionada por el modelo de riesgo.

### Salida larga
1. Activa solo mientras la posición neta es larga.
2. La vela debe abrir por encima de la media móvil *Buy Close* y cerrar de vuelta por debajo de ella, señalando un cruce bajista.
3. Cuando se activa, toda la posición se cierra con una orden de mercado.

### Entrada corta
1. La vela debe cerrar por debajo de la media móvil *Sell Open* después de abrir por encima de ella.
2. `UseSell` debe estar habilitado.
3. Si `ConsiderPriceLastOut` está habilitado, el precio actual debe ser mayor o igual al último precio de salida. Esto evita ponerse corto por debajo de la cobertura previa.
4. Se envía una orden de venta a mercado utilizando el volumen basado en el riesgo.

### Salida corta
1. Activa solo mientras la posición es corta.
2. La vela debe abrir por debajo de la media móvil *Sell Close* y cerrar por encima de ella.
3. La posición corta se cubre completamente a mercado.

## Riesgo y dimensionamiento de posición
- `MaximumRisk` expresa el capital de riesgo por operación como una fracción del capital del portafolio. La estrategia divide este monto de riesgo por el precio actual para obtener una estimación de volumen bruto.
- `DecreaseFactor` emula la reducción de lote original de MetaTrader. Después de dos o más operaciones perdedoras consecutivas, el volumen se reduce proporcionalmente a la racha de pérdidas dividida por `DecreaseFactor`.
- Los volúmenes se alinean al paso de volumen del instrumento y nunca caen por debajo de un paso. Si el cálculo de riesgo falla, el valor de respaldo es la propiedad `Volume` de la estrategia (por defecto 1 contrato/lote).

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `MaximumRisk` | `0.02` | Fracción del capital arriesgado por operación. |
| `DecreaseFactor` | `3` | Factor de reducción de lote después de pérdidas consecutivas. Use `0` para deshabilitar. |
| `BuyOpenPeriod` | `30` | Período de la media móvil usada para entradas largas. |
| `BuyOpenShift` | `3` | Desplazamiento hacia adelante (barras) aplicado a la media móvil de entrada larga. |
| `BuyOpenMethod` | `Exponential` | Método de media móvil para entradas largas (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). |
| `BuyOpenPrice` | `Close` | Entrada de precio para la media móvil de entrada larga. |
| `BuyClosePeriod` | `14` | Período de la media móvil de salida larga. |
| `BuyCloseShift` | `3` | Desplazamiento (barras) aplicado a la media móvil de salida larga. |
| `BuyCloseMethod` | `Exponential` | Método de la media móvil de salida larga. |
| `BuyClosePrice` | `Close` | Entrada de precio para la media móvil de salida larga. |
| `SellOpenPeriod` | `30` | Período de la media móvil de entrada corta. |
| `SellOpenShift` | `0` | Desplazamiento (barras) aplicado a la media móvil de entrada corta. |
| `SellOpenMethod` | `Exponential` | Método de la media móvil de entrada corta. |
| `SellOpenPrice` | `Close` | Entrada de precio para la media móvil de entrada corta. |
| `SellClosePeriod` | `20` | Período de la media móvil de salida corta. |
| `SellCloseShift` | `2` | Desplazamiento (barras) aplicado a la media móvil de salida corta. |
| `SellCloseMethod` | `Exponential` | Método de la media móvil de salida corta. |
| `SellClosePrice` | `Close` | Entrada de precio para la media móvil de salida corta. |
| `UseBuy` | `true` | Habilitar o deshabilitar operaciones largas. |
| `UseSell` | `true` | Habilitar o deshabilitar operaciones cortas. |
| `ConsiderPriceLastOut` | `true` | Requerir mejora de precio respecto a la última salida antes de re-entrar. |
| `CandleType` | Marco temporal 15m | Serie de velas usada para cálculos. |

## Notas adicionales
- El último precio de salida y el contador de pérdidas consecutivas se rastrean desde las ejecuciones de operaciones, reflejando el comportamiento de MetaTrader.
- Debido a que StockSharp ejecuta en velas terminadas, el filtro de precio de entrada compara con el precio de cierre de la vela, lo que aproxima la comparación original de ask/bid basada en ticks.
- La estrategia asume una cuenta de netting; no se soporta cobertura de múltiples posiciones simultáneamente.
- Siempre valide la configuración con pruebas históricas antes de operar con capital real.
