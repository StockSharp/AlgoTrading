# Estrategia Heiken Ashi Idea
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia reproduce el comportamiento del asesor experto original **HeikenAshiIdea.mq4** usando la API de alto nivel de StockSharp. Espera señales alcistas o bajistas alineadas en dos marcos temporales de velas Heikin Ashi y luego coloca órdenes límite pendientes a una distancia configurable del mercado. El objetivo es capturar movimientos de continuación fuerte cuando la vela Heikin Ashi más reciente no tiene mecha en contra de la dirección de la tendencia.

## Lógica de trading

1. **Reconstrucción Heikin Ashi** – la estrategia reconstruye internamente velas Heikin Ashi para el marco temporal de trading primario y para un marco temporal de confirmación superior. Para cada marco temporal se almacenan las últimas dos velas Heikin Ashi de modo que se pueda analizar la dirección del cuerpo y la presencia de mechas.
2. **Condición de ruptura** – aparece un setup largo cuando ambos marcos temporales muestran:
   - la vela Heikin Ashi más reciente es alcista y su apertura es igual al mínimo (sin sombra inferior), y
   - la vela Heikin Ashi anterior también es alcista pero tiene sombra inferior.
   Un setup corto requiere las condiciones bajistas simétricas (sin sombra superior en la última vela y sombra superior en la anterior).
3. **Filtro de volatilidad ATR** – el Average True Range con longitud configurable debe estar subiendo (`ATR[t] > ATR[t-1]`) si el filtro está habilitado. Esto reproduce la verificación de volatilidad `ActiveMarket` original.
4. **Ventana de trading** – las señales se ignoran fuera de la sesión de trading definida por el usuario (por defecto 09:00–19:00).
5. **Colocación de órdenes** – cuando una señal es válida la estrategia coloca una única orden límite pendiente:
   - Señal larga → orden de compra límite en `ClosePrice - DistancePoints * PriceStep`.
   - Señal corta → orden de venta límite en `ClosePrice + DistancePoints * PriceStep`.
   Las órdenes pendientes opuestas existentes se cancelan antes de encolar una nueva orden. La estrategia rastrea solo una orden pendiente por dirección y limpia automáticamente las referencias cuando la orden queda inactiva.
6. **Gestión de posición** – las distancias opcionales de take-profit y stop-loss se traducen en mecanismos protectores de StockSharp mediante `StartProtection`. Cuando se abre una nueva vela del marco temporal de "cerrar todo", la estrategia cancela todas las órdenes pendientes y cierra cualquier posición abierta si el indicador está habilitado. Esto imita el comportamiento `UseCloseAll` del EA original.

## Gestión de riesgos

- Los niveles protectores se expresan en pasos de precio (puntos) para mantenerse cerca de la implementación MetaTrader. Son opcionales; usar `0` deshabilita la protección correspondiente.
- Las órdenes pendientes solo se colocan cuando la distancia calculada es positiva y el volumen de trading es mayor que cero.
- La estrategia nunca promedia posiciones automáticamente; primero aplana la orden pendiente opuesta antes de programar una nueva.
- Se usa una tolerancia igual a la mitad del paso de precio del instrumento cuando se verifica si las velas Heikin Ashi tienen o no mechas. Esto previene problemas de redondeo en punto flotante al mismo tiempo que se mantiene fiel a las comparaciones estrictas originales.

## Parámetros

| Nombre | Descripción | Valor predeterminado |
| --- | --- | --- |
| `DistancePoints` | Distancia en pasos de precio para las órdenes límite pendientes. | `8` |
| `StopLossPoints` | Distancia de stop-loss en pasos de precio (0 deshabilita el stop). | `0` |
| `TakeProfitPoints` | Distancia de take-profit en pasos de precio (0 deshabilita el objetivo). | `20` |
| `UseCloseAllOnNewBar` | Cerrar posición y cancelar órdenes cuando se abre una nueva vela del marco temporal de cierre. | `true` |
| `CandleType` | Tipo de vela primaria usada para las señales de trading. | Marco temporal `30m` |
| `HigherCandleType` | Tipo de vela de confirmación para el filtro multi-marco temporal. | Marco temporal `1d` |
| `CloseAllCandleType` | Tipo de vela que activa la rutina de cierre total. | Marco temporal `7d` |
| `StartHour` | Primera hora de la sesión de trading (inclusive). | `9` |
| `EndHour` | Última hora de la sesión de trading (inclusive). | `19` |
| `UseAtrFilter` | Habilitar el filtro de volatilidad creciente ATR. | `true` |
| `AtrPeriod` | Período ATR usado por el filtro de volatilidad. | `14` |

## Notas adicionales

- La estrategia usa la propiedad `Volume` incorporada de `Strategy` como tamaño de orden base. Ajústela antes de iniciar la estrategia.
- Dado que la implementación de StockSharp usa precios de cierre de velas para la colocación de órdenes pendientes, la ejecución en vivo puede diferir ligeramente del código MT4 original que usaba cotizaciones bid/ask, pero la idea central permanece intacta.
- Para extender la lógica a diferentes mercados simplemente ajuste los tipos de velas, la ventana de trading y los parámetros de distancia manteniendo la confirmación multi-marco temporal en su lugar.
