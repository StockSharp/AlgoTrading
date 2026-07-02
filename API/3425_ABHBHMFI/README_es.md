# ABH_BH_MFI Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **ABH_BH_MFI** es una StockSharp adaptación de alto nivel del MetaTrader asesor experto "Expert_ABH_BH_MFI". El algoritmo combina patrones de velas Harami alcistas y bajistas con la confirmación del Índice de flujo de dinero (MFI). Las operaciones largas se activan cuando se forma un Harami alcista dentro de un mercado en caída mientras la IMF permanece deprimida. Las operaciones cortas requieren un Harami bajista dentro de un mercado en crecimiento y una IMF elevada. La implementación original de MQL se basó en la infraestructura de señal de MetaTrader; esta conversión mantiene la lógica de decisión pero la expresa con las suscripciones de velas, el enlace de indicadores y los asistentes de gestión de posiciones de StockSharp.

## Lógica de trading
### 1. Detección de patrones Harami
- La estrategia almacena las dos velas completadas más recientes.
- Un **Harami alcista** requiere:
  - Hace dos velas había una vela larga negra (bajista) cuyo cuerpo es más grande que la longitud promedio del cuerpo.
  - La vela más reciente es alcista y su apertura/cierre están envueltos por el cuerpo de la vela bajista anterior.
  - El punto medio de la vela más antigua se encuentra por debajo del promedio móvil simple de cierres, lo que indica una tendencia bajista predominante.
- Un **Harami bajista** refleja estos requisitos con colores invertidos y el punto medio por encima del promedio móvil para confirmar una tendencia alcista.

### 2. Confirmación del índice de flujo de dinero
- La MFI utiliza el `MfiPeriod` configurable (predeterminado **37**) para replicar la configuración original del oscilador.
- Las entradas largas exigen que el último valor de MFI completado se mantenga por debajo de `BullishThreshold` (predeterminado **40**) para garantizar el agotamiento de la entrada de capital.
- Las entradas cortas requieren que la IMF se mantenga por encima de `BearishThreshold` (predeterminado **60**) para mostrar el agotamiento de la presión de compra.

### 3. Reglas de salida a través de cruces de IMF
- Las posiciones largas activas se cierran cuando la IMF cruza por encima de `ExitLowerLevel` (predeterminado **30**) o `ExitUpperLevel` (predeterminado **70**), coincidiendo con las MetaTrader condiciones `MFI(1) > level && MFI(2) < level`.
- Las posiciones cortas activas se cierran cuando la IMF cruza hacia abajo desde la zona de sobrecompra o sube por debajo del nivel de sobreventa, reflejando las cláusulas de salida cortas originales.

### 4. Gestión de riesgos
- La estrategia aplica opcionalmente `StartProtection` con compensaciones de stop-loss y take-profit expresadas en incrementos de precio. Poner a cero el parámetro correspondiente desactiva la distancia de protección, reproduciéndose los valores predeterminados de MetaTrader.
- El tamaño de la posición utiliza la propiedad base `Volume`; Revertir posiciones automáticamente agrega suficientes contratos para aplanarse y reabrirse en la nueva dirección, al igual que el experto en fuentes.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | plazo de 1 hora | Serie de velas primarias analizadas en busca de patrones e IMF. |
| `MfiPeriod` | 37 | Retrospectiva del indicador del índice de flujo de dinero. |
| `BodyAveragePeriod` | 11 | Longitud de las medias móviles simples que miden el tamaño corporal y la tendencia de cierre. |
| `BullishThreshold` | 40 | Valor máximo de MFI permitido antes de abrir operaciones largas. |
| `BearishThreshold` | 60 | Valor mínimo de MFI requerido antes de abrir operaciones cortas. |
| `ExitLowerLevel` | 30 | Nivel de cruce de MFI inferior para salidas de posición. |
| `ExitUpperLevel` | 70 | Nivel de cruce superior de MFI para salidas de posición. |
| `StopLossPoints` | 0 | Distancia de stop-loss opcional en pasos de precio (0 desactiva). |
| `TakeProfitPoints` | 0 | Distancia de toma de ganancias opcional en pasos de precio (0 inhabilitaciones). |

## Notas de implementación
- Los datos de la vela se reciben a través de `SubscribeCandles(CandleType)` y se procesan solo cuando el estado de la vela es `Finished`, lo que garantiza la alineación con la lógica de barra cerrada del experto MQL.
- El indicador MFI está vinculado directamente con `.Bind(_mfi, ProcessCandle)` para que el controlador reciba valores decimales listos para usar sin llamar a `GetValue`.
- Dos promedios móviles simples auxiliares replican las funciones auxiliares `AvgBody` y `CloseAvg` del código MetaTrader. Sus resultados se almacenan en caché para evitar consultas de indicadores históricos.
- Para tomar decisiones de entrada y salida, llame a `IsFormedAndOnlineAndAllowTrading()` antes de enviar órdenes, manteniendo la coherencia con las comprobaciones de seguridad comerciales recomendadas por StockSharp.

## Diferencias con el experto MetaTrader
- La gestión del dinero se simplifica al volumen de la estrategia base. El módulo original de "lote fijo" se tradujo al asistente de dimensionamiento de posiciones de StockSharp, que cubre la misma funcionalidad sin clases separadas.
- El componente de trailing stop MetaTrader (`TrailingNone`) no tenía lógica; por lo tanto, la versión StockSharp omite cualquier acción posterior pero mantiene objetivos de riesgo fijos opcionales.
- El registro es mínimo de forma predeterminada; puede ampliarlo con llamadas `LogInfo` si necesita diagnósticos comerciales detallados.

## Consejos de uso
1. Configure la seguridad deseada y asigne el `CandleType` antes de iniciar la estrategia.
2. Opcionalmente, ajuste la IMF y los umbrales de salida para adaptarlos a diferentes regímenes de volatilidad.
3. Proporcionar `StopLossPoints`/`TakeProfitPoints` distinto de cero cuando el corredor requiera órdenes de protección explícitas; de lo contrario, déjelos en cero para comerciar sin objetivos concretos.
4. Supervise los paneles de gráficos creados por la estrategia para visualizar velas, el indicador MFI y las operaciones ejecutadas.
