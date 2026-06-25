# Exp XWAMI MMRec (ID 2956)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen

La estrategia replica el asesor experto de MetaTrader **Exp_XWAMI_MMRec**, combinando el indicador de momentum personalizado XWAMI con un "contador" de gestión de dinero. El momentum se mide como la diferencia entre el precio actual y el precio `Period` barras atrás. Esa diferencia pasa por cuatro etapas de suavizado configurables; las etapas tercera y cuarta forman los buffers `Up` y `Down` del indicador original. Los cruces entre los dos buffers impulsan las reversiones de posición.

Cada etapa puede emular varios algoritmos de suavizado: medias móviles simples/exponenciales/suavizadas/ponderadas linealmente, Jurik JJMA/JurX, Tillson T3, VIDYA (aproximado con EMA) y AMA de Kaufman. La estrategia trabaja con una única posición agregada y admite operaciones tanto largas como cortas. El riesgo se reduce después de pérdidas consecutivas comparando los resultados recientes de las operaciones con las ventanas `BuyTotalTrigger`/`SellTotalTrigger` y contando las pérdidas relativas a `BuyLossTrigger`/`SellLossTrigger`.

Los stops de protección siguen la implementación de MetaTrader: `StopLossPoints` y `TakeProfitPoints` se miden en puntos del símbolo (`Security.PriceStep`). Cuando se toca un stop o un objetivo dentro del marco temporal de señal, la posición se cierra inmediatamente y el resultado de la operación ingresa al historial de gestión de dinero.

## Parámetros

| Propiedad StockSharp | Predeterminado | Entrada original | Descripción |
| --- | --- | --- | --- |
| `CandleType` | Marco temporal H1 | `InpInd_Timeframe` | Marco temporal usado para construir velas para el indicador. |
| `Period` | 1 | `iPeriod` | Distancia (en barras) entre el precio actual y el precio de comparación en el cálculo del momentum. |
| `Method1` / `Length1` / `Phase1` | `T3`, `4`, `15` | `XMethod1`, `XLength1`, `XPhase1` | Método de suavizado, longitud y fase para la etapa 1. La fase solo la usan Jurik/JurX/T3. |
| `Method2` / `Length2` / `Phase2` | `Jjma`, `13`, `15` | `XMethod2`, `XLength2`, `XPhase2` | Configuración para la segunda etapa de suavizado. |
| `Method3` / `Length3` / `Phase3` | `Jjma`, `13`, `15` | `XMethod3`, `XLength3`, `XPhase3` | Configuración para la tercera etapa (buffer `Up` del indicador). |
| `Method4` / `Length4` / `Phase4` | `Jjma`, `4`, `15` | `XMethod4`, `XLength4`, `XPhase4` | Configuración para la cuarta etapa (buffer `Down` del indicador). |
| `AppliedPrice` | `Close` | `IPC` | Fuente de precio enviada al cálculo del momentum. Se reproducen todas las opciones de precio de MetaTrader, incluyendo ambos sabores de TrendFollow y el precio Demark. |
| `SignalBar` | 1 | `SignalBar` | Índice de la vela histórica usada para evaluar los cruces (`0` = barra terminada más reciente). |
| `AllowBuyOpen` / `AllowSellOpen` | `true` | `BuyPosOpen`, `SellPosOpen` | Habilita entradas largas o cortas respectivamente. |
| `AllowBuyClose` / `AllowSellClose` | `true` | `BuyPosClose`, `SellPosClose` | Habilita salidas forzadas cuando aparece la señal opuesta. |
| `NormalVolume` | `0.1` | `MM` | Tamaño de lote/volumen predeterminado usado después de series rentables o neutras. |
| `ReducedVolume` | `0.01` | `SmallMM_` | Lote reducido aplicado después de demasiadas pérdidas. |
| `BuyTotalTrigger` / `BuyLossTrigger` | `5` / `3` | `BuyTotalMMTriger`, `BuyLossMMTriger` | Número de operaciones largas recientes inspeccionadas y máximo de pérdidas dentro de esa ventana antes de reducir el volumen largo. |
| `SellTotalTrigger` / `SellLossTrigger` | `5` / `3` | `SellTotalMMTriger`, `SellLossMMTriger` | Misma lógica para posiciones cortas. |
| `StopLossPoints` | `1000` | `StopLoss_` | Distancia del stop-loss en puntos. |
| `TakeProfitPoints` | `2000` | `TakeProfit_` | Distancia del take-profit en puntos. |

## Comportamiento

1. Suscribirse a la serie de velas solicitada y evaluar solo velas terminadas.
2. Calcular la diferencia de precio (`AppliedPrice` ahora vs. `Period` barras atrás). Cuando haya suficiente historial, pasar la diferencia por las cuatro etapas de suavizado.
3. Almacenar las salidas de las etapas tercera (`Up`) y cuarta (`Down`). Cuando `Up` y `Down` en `SignalBar + 1` (la barra anterior) cruzan, la estrategia cambia el sesgo. Si `Up > Down`, se cierran posiciones cortas y se abre una posición larga si `Up <= Down` en la barra de señal. La lógica opuesta maneja señales bajistas.
4. El tamaño de posición lo selecciona el contador: se inspeccionan los últimos `BuyTotalTrigger` (o `SellTotalTrigger`) beneficios de operaciones. Si al menos `BuyLossTrigger` (o `SellLossTrigger`) de ellos son negativos, la próxima operación usa `ReducedVolume`; de lo contrario se usa `NormalVolume`.
5. Cuando existe una posición larga, las distancias de stop-loss y take-profit se convierten de puntos a precio multiplicando por `Security.PriceStep`. Al romperse, la posición se cierra al precio de stop/objetivo y la operación se registra para el módulo de gestión de dinero. Las operaciones cortas siguen las reglas simétricas.

## Diferencias con la versión MetaTrader

- StockSharp agrega posiciones, por lo que `BuyMagic`/`SellMagic`, la contabilidad de variables globales de MetaTrader y la opción `MarginMode` son innecesarias y se omitieron.
- Tillson T3 se implementa explícitamente; Jurik JJMA y JurX se asignan a `JurikMovingAverage` con la fase proporcionada. VIDYA y ParMA se aproximan con una media móvil exponencial porque StockSharp carece de equivalentes nativos.
- Las órdenes se ejecutan con `BuyMarket`/`SellMarket` y los stops/objetivos se aplican monitoreando los máximos/mínimos de velas en lugar de por órdenes stop nativas de MT5.
- La entrada de desviación/deslizamiento no es necesaria en los modelos de ejecución de StockSharp y se eliminó.

## Notas de uso

1. Elija el instrumento y establezca `CandleType` en el marco temporal usado por el experto original.
2. Configure los métodos y longitudes de suavizado para coincidir con la configuración del indicador MetaTrader.
3. Ajuste `NormalVolume`, `ReducedVolume` y los umbrales de activación para alinearse con la política de riesgo deseada.
4. Adjunte la estrategia a una cartera e iníciela; el trading está totalmente automatizado y se revierte en cada cruce del indicador.

Para mayor personalización puede editar los mapeos de suavizado dentro de `ExpXwamiMmRecStrategy.CreateFilter` para conectar indicadores alternativos de StockSharp.
