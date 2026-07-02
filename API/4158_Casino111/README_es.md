# Estrategia Casino111
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Casino111 es un sistema de ruptura contratendencia que se origina en los MetaTrader 4 asesores expertos con el mismo nombre. En cada nueva barra, la estrategia compara el precio de apertura actual con los niveles de referencia derivados de la vela diaria anterior. Si se abren brechas más allá de los extremos diarios (más buffers configurables), el algoritmo abre inmediatamente una posición de mercado en la dirección opuesta y se basa en una protección simétrica de stop-loss/take-profit. El puerto StockSharp mantiene el comportamiento de posición única del robot original y agrega una amplia parametrización para investigación y optimización.

## Lógica de entrada y salida
1. El máximo y mínimo diario anterior se recuperan de una suscripción de vela diaria dedicada. Dos desplazamientos (`UpperOffsetPoints` y `LowerOffsetPoints`) expresados ​​en MetaTrader puntos amplían el canal de referencia.
2. En cada vela comercial terminada, la estrategia inspecciona las aperturas anteriores y actuales:
   - Cuando la nueva apertura salta por encima del máximo diario más el desplazamiento superior, se abre una posición **corta** (desvanecimiento de la brecha).
   - Cuando la nueva apertura cae por debajo del mínimo diario menos el desplazamiento inferior, se abre una posición **larga**.
3. Sólo se permite una posición a la vez. Cualquier orden activa debe completarse antes de que se considere una nueva señal.
4. `StartProtection` refleja el stop fijo original y el objetivo de toma, ambos ubicados a `BetPoints` del precio de entrada (convertidos en pasos de precio).

## gestión del dinero
- `UseMoneyManagement = false` mantiene fijo el tamaño de la operación (`BaseVolume`).
- `UseMoneyManagement = true` activa la progresión de martingala que se ve en el código MT4:
  - Después de cada operación perdedora o de equilibrio, el siguiente volumen de orden se multiplica por `(BetPoints * 2) / (BetPoints - spreadPoints)`.
  - El diferencial se estima a partir de las últimas mejores cotizaciones de oferta y demanda recopiladas a través de la suscripción al libro de pedidos. Cuando no hay cotizaciones disponibles, el multiplicador predeterminado es `2`.
  - Las victorias restablecen el tamaño de la posición a `BaseVolume`. Todos los volúmenes están alineados con el instrumento `VolumeStep` y limitados por `MaxVolume`.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `EnableBuy` | `bool` | `true` | Permitir entradas largas provocadas por huecos por debajo del canal diario. |
| `EnableSell` | `bool` | `true` | Permitir entradas cortas provocadas por brechas por encima del canal diario. |
| `BetPoints` | `decimal` | `400` | Distancia simétrica de stop-loss y take-profit en MetaTrader puntos (convertida en pasos de precio para StockSharp). |
| `UpperOffsetPoints` | `decimal` | `97` | Se agregó un amortiguador por encima del máximo diario anterior para detectar reversiones de la brecha bajista. |
| `LowerOffsetPoints` | `decimal` | `77` | El amortiguador se restó por debajo del mínimo diario anterior para detectar reversiones de la brecha alcista. |
| `UseMoneyManagement` | `bool` | `false` | Habilite la progresión de lotes estilo martingala. |
| `MaxVolume` | `decimal` | `4` | Límite aplicado al volumen calculado cuando la gestión del dinero está activa. |
| `BaseVolume` | `decimal` | `0.1` | Tamaño de orden inicial utilizado después de una operación rentable o cuando la administración del dinero está deshabilitada. |
| `CandleType` | `DataType` | `H1` | Plazo principal utilizado para evaluar las condiciones de la brecha abierta (el valor predeterminado es 1 hora). |
| `DailyCandleType` | `DataType` | `D1` | Tipo de vela que suministra el máximo/mínimo del día anterior (el valor predeterminado es 1 día). |

## Notas de implementación
- La estrategia se basa en el API de alto nivel de StockSharp: `SubscribeCandles` proporciona flujos comerciales y diarios, mientras que `SubscribeOrderBook` mantiene el diferencial más reciente para el multiplicador de administración de dinero.
- `StartProtection` gestiona tanto el stop-loss como el take-profit, por lo que cada entrada recibe inmediatamente salidas simétricas como en MT4.
- Los comentarios en línea en inglés resaltan cada punto de decisión para facilitar el mantenimiento.
- Todos los cálculos evitan búsquedas en el historial de indicadores; solo se requieren los valores de apertura de velas actuales, lo que refleja la lógica `Time[0]` / `Open[0]` de MetaTrader.

## Consejos de uso
- Elija un período de negociación que coincida con su estudio. Las velas predeterminadas de una hora replican la configuración común de MT4, pero se puede suministrar cualquier `DataType` compatible con StockSharp.
- Cuando utilice la administración de dinero, asegúrese de que `MaxVolume` respete los límites del corredor; el ayudante de alineación fija el resultado en `VolumeStep`, `MinVolume` y `MaxVolume`.
- Debido a que el sistema siempre mantiene abierta como máximo una posición, se combina bien con gráficos StockSharp que trazan marcadores de entrada/salida para inspección manual.
- Pruebe la estrategia dentro de un entorno de repetición antes de conectarla a un lugar en vivo; el enfoque de desvanecimiento de brechas es agresivo y depende de diferenciales confiables.
