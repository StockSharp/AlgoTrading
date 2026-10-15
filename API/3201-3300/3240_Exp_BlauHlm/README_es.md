# Estrategia de Exp BlauHlm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de Exp BlauHlm** es un port de StockSharp del asesor experto de MetaTrader 5 `Exp_BlauHLM.mq5`. El sistema se basa en el oscilador Blau High-Low Momentum (HLM) que compara máximos y mínimos recientes, suaviza la diferencia con un pipeline XMA configurable y reacciona a tres modos de operación distintos:

- **Breakdown** – opera una ruptura de la línea cero del componente del histograma.
- **Twist** – busca giros de momento dentro del histograma para capturar transiciones de pendiente.
- **CloudTwist** – trabaja con los envolventes superior e inferior producidos por el indicador y reacciona a los cruzamientos de "nube".

La implementación en StockSharp mantiene los mismos parámetros, valores predeterminados y reglas de trading mientras traduce los detalles de gestión de dinero a la propiedad genérica `Volume` de la estrategia base.

## Lógica de trading

1. Para cada vela terminada del marco temporal configurado, la estrategia calcula el oscilador Blau HLM:
   - Calcular la diferencia entre el máximo más reciente y el máximo `XLength - 1` barras atrás y una diferencia en espejo para mínimos.
   - Limitar las contribuciones negativas a cero y restarlas para obtener el valor HLM crudo (expresado en puntos cuando el instrumento especifica un tamaño de tick).
   - Suavizar la secuencia a través de cuatro medias móviles en cascada con métodos idénticos pero longitudes independientes.
2. Dependiendo del **Mode** seleccionado:
   - **Breakdown** abre una posición larga cuando el valor del histograma anterior es positivo y el nuevo no es positivo (recuperación de línea cero) y cierra cortos en la misma situación. Una regla simétrica maneja entradas cortas/salidas largas cuando el histograma cambia de negativo a no negativo.
   - **Twist** compara la pendiente del histograma a través de tres puntos históricos. Una aceleración local (valor medio subiendo después de una bajada) activa la lógica larga, mientras que una desaceleración (valor medio bajando después de una subida) activa la lógica corta.
   - **CloudTwist** monitorea los dos envolventes suavizados. Cuando la banda superior anterior está por encima de la inferior y los nuevos valores se cruzan por debajo/encima entre sí, se producen señales largas o cortas respectivamente.
3. La gestión de posiciones sigue los permisos `BuyOpen`, `SellOpen`, `BuyClose`, `SellClose` y usa el `Volume` de la estrategia para entradas de mercado. Las señales opuestas cierran las posiciones existentes antes de abrir una nueva.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
| ------ | ---- | -------------- | ----------- |
| `CandleType` | `DataType` | Velas `H4` | Marco temporal procesado por el oscilador. |
| `SmoothingMethod` | `SmoothMethod` | `Exponential` | Método de media móvil para cada etapa de suavizado (los modos legacy no soportados recurren a EMA). |
| `XLength` | `int` | `2` | Período en barras usado para medir el momento crudo alto/bajo. |
| `FirstLength` | `int` | `20` | Período de la primera etapa de suavizado. |
| `SecondLength` | `int` | `5` | Período de la segunda etapa de suavizado. |
| `ThirdLength` | `int` | `3` | Período de la tercera etapa de suavizado. |
| `FourthLength` | `int` | `3` | Período del suavizador de señal final. |
| `Phase` | `int` | `15` | Parámetro de fase Jurik (limitado a ±100, ignorado por suavizadores no Jurik). |
| `SignalBar` | `int` | `1` | Desplazamiento histórico usado al comparar valores del indicador. |
| `EntryMode` | `Mode` | `Twist` | Lógica de trading copiada del experto MQL (`Breakdown`, `Twist`, `CloudTwist`). |
| `BuyOpen` / `SellOpen` | `bool` | `true` | Permitir abrir posiciones largas/cortas. |
| `BuyClose` / `SellClose` | `bool` | `true` | Permitir cerrar posiciones largas/cortas cuando aparece una señal opuesta. |

## Notas de conversión

- La biblioteca MQL `SmoothAlgorithms.mqh` incluye filtros propietarios (JJMA, JurX, ParMA, T3, VIDYA, AMA). StockSharp proporciona alternativas integradas para las variantes más comunes, por lo tanto los modos no soportados se aproximan con la media móvil exponencial para mantener el flujo de trabajo intacto.
- Los parámetros de gestión de dinero (`MM`, `MarginMode`, `StopLoss`, `TakeProfit`, `Deviation`) controlan el tamaño de la orden y la ejecución en MetaTrader. En este port, la propiedad genérica `Volume` define el tamaño de la posición y las órdenes siempre se envían a mercado.
- El tiempo de señal refleja el desplazamiento `SignalBar` usado por el experto original: la estrategia mantiene un buffer circular interno de valores del indicador y realiza comparaciones en instantáneas históricas para que los resultados de optimización permanezcan consistentes.
- La protección de riesgo se delega a `StartProtection()`; configure reglas globales de stop-loss/take-profit en la estrategia padre o conector de trading si se requiere.

## Consejos de uso

1. Establecer la propiedad `Volume` antes de iniciar la estrategia para definir el número de lotes/contratos por operación.
2. Para símbolos sin un `PriceStep` significativo, el oscilador trabaja en unidades de precio crudas. Considere reescalar los parámetros si el activo usa tamaños de tick grandes.
3. Al experimentar con suavizadores no exponenciales, recuerde que longitudes muy cortas combinadas con extremos de fase Jurik pueden llevar a señales inestables; amplíe los períodos para mayor estabilidad.
4. Combine la estrategia con controles de riesgo a nivel de cartera o las reglas de protección integradas para emular el comportamiento original de stop-loss/take-profit.
