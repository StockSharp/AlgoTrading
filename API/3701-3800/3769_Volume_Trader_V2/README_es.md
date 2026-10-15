# Estrategia Volume Trader V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Volume Trader V2 es una conversión directa del MetaTrader asesor experto `Volume_trader_v2_www_forex-instruments_info.mq4`. El sistema original observa cómo evoluciona el volumen total de las últimas velas y utiliza este flujo a corto plazo para decidir si debe estar activa una simple exposición larga o corta. El puerto StockSharp mantiene el comportamiento de una posición a la vez, el filtro de hora del día y el requisito de actuar solo una vez por vela completa.

La estrategia se suscribe a una serie de velas configurables y almacena en caché el volumen de las dos últimas velas terminadas. Cuando se cierra una nueva barra, los volúmenes de las dos barras anteriores (MetaTrader's `Volume[1]` y `Volume[2]`) se comparan y se produce una dirección comercial actualizada:

- `Volume[1] < Volume[2]` genera un sesgo **largo**.
- `Volume[1] > Volume[2]` genera un sesgo **corto**.
- Volúmenes iguales u horarios de negociación desactivados eliminan cualquier exposición abierta.

Antes de enviar una nueva orden, la posición actual se aplana si apunta en la dirección opuesta para que la implementación de StockSharp coincida con el ciclo de vida de la orden MetaTrader.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | marco de tiempo de 5 minutos | Tipo de datos solicitado de `SubscribeCandles`. Configúrelo para que coincida con el período del gráfico utilizado en MetaTrader. |
| `StartHour` | 8 | Primera hora de negociación (inclusive). Las señales fuera de la ventana se ignoran y se cierra cualquier posición. |
| `EndHour` | 20 | Última hora de negociación (inclusive). Cuando la vela actual comienza después de esta hora, la estrategia se mantiene plana. |
| `TradeVolume` | 0.1 | Tamaño de lote replicado de EA. El valor también se asigna a `Strategy.Volume` por lo que los métodos auxiliares utilizan la misma cantidad. |

Todos los parámetros son instancias `StrategyParam<T>` regulares, por lo que pueden optimizarse o exponerse a través de la interfaz de usuario.

## Lógica de trading
1. Manipule únicamente velas terminadas para garantizar la paridad barra por barra con el EA.
2. Almacene en caché los equivalentes de `Volume[1]` y `Volume[2]` en `_previousVolume` y `_twoBarsAgoVolume` antes de cualquier evaluación de señal.
3. Valide que la hora de inicio de la vela sea entre `StartHour` y `EndHour` (inclusive). Fuera de este rango, cualquier posición activa se cierra y no se crean nuevas órdenes.
4. Calcule la dirección deseada:
   - Largo cuando el volumen más reciente es inferior al de la barra anterior.
   - Corto cuando el volumen más reciente es superior a la barra anterior.
   - Neutral en caso contrario.
5. Si la dirección deseada difiere de la posición actual, cierre primero la posición opuesta (`BuyMarket(-Position)` o `SellMarket(Position)`).
6. Ingrese la nueva posición usando el `TradeVolume` configurado solo cuando la estrategia sea plana o esté posicionada en la dirección opuesta.
7. Actualice los volúmenes almacenados en caché para que el siguiente ciclo aún compare las dos últimas velas completadas.

Este flujo garantiza que no se realicen órdenes mientras una vela aún se esté construyendo y que la estrategia StockSharp reaccione exactamente una vez por barra, al igual que la implementación MetaTrader que se basó en `LastBarChecked`.

## Notas adicionales
- Se llama a `StartProtection()` en `OnStarted` para reutilizar el asistente de protección del marco que realiza un seguimiento de la posición actual.
- La propiedad `Comment` refleja los mensajes de diagnóstico EA (`"Up trend"`, `"Down trend"`, `"No trend..."` o `"Trading paused"`) para simplificar la supervisión.
- La estrategia no mantiene colecciones adicionales y aprovecha la suscripción de vela de alto nivel API de acuerdo con las pautas del proyecto.
- Establezca el tipo de vela, la seguridad y el volumen para que coincidan con el instrumento y el período de tiempo utilizados originalmente en MetaTrader para obtener resultados comparables.
