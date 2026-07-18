# Stochastic Estrategia de aceleración
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia del acelerador Stochastic es una conversión del MetaTrader 5 experto *#2 stoch mt5*. El robot original evalúa tres
osciladores estocásticos junto con Bill Williams' Accelerator Oscillator y Awesome Oscillator. Se abre una posición larga
sólo cuando todos los filtros estocásticos estén de acuerdo en el impulso alcista y el Oscilador Acelerador cruce por encima de un umbral de sensibilidad.
Las posiciones cortas utilizan las reglas simétricas. Una vez que se ejecuta una operación, Awesome Oscillator monitorea las reversiones de impulso para cerrar
la exposición. El puerto StockSharp reproduce esta mecánica mientras depende de la suscripción de vela de alto nivel API y
fijaciones del indicador.

La estrategia mantiene el perfil de administración de dinero del EA. Las entradas se dimensionan con un monto de lote fijo, mientras que el stop-loss y
las distancias de obtención de beneficios se expresan en MetaTrader pips. La implementación StockSharp usa `StartProtection` por lo que la configuración
Los límites de riesgo se adjuntan automáticamente a cada nueva posición. Los pasos de precio se convierten a MetaTrader unidades de pip para mantener el
mismas distancias de protección entre corredores.

## Lógica comercial
1. Suscríbase a la serie de velas principal definida por `CandleType` y procese solo velas terminadas, reflejando el EA original.
2. Alimenta tres instancias `StochasticOscillator`:
   - La **señal estocástica** comprueba si %K está por encima o por debajo de %D.
   - El **estocástico de entrada** valida que las señales alcistas permanezcan por encima de `EntryLevel` (o por debajo de `100 - EntryLevel` para cortos).
   - El **filtro estocástico** garantiza que las configuraciones alcistas permanezcan por debajo de `FilterLevel` (o por encima de `100 - FilterLevel` para cortos).
3. Realice un seguimiento del oscilador del acelerador y solicite que cruce por encima de `AcceleratorLevel` para confirmar entradas largas. Los pantalones cortos exigen un
cruce debajo de `-AcceleratorLevel`.
4. Cierre cualquier posición abierta cuando Awesome Oscillator vuelva a cruzar la banda `AwesomeLevel` en la dirección opuesta.
5. Después de aplanar, abra una nueva posición si exactamente un lado satisface todos los filtros de entrada. El volumen se ajusta a la seguridad.
paso de lote para que la solicitud siga siendo válida para corredores reales.
6. Aplique distancias de stop-loss y take-profit usando `StartProtection`, manteniendo los mismos controles de riesgo basados en pips que el MetaTrader
experto.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | plazo de 4 horas | Velas primarias procesadas por la estrategia. |
| `TradeVolume` | `decimal` | `0.01` | Volumen utilizado para nuevas entradas (lotes). |
| `StopLossPips` | `decimal` | `40` | Distancia de stop-loss en MetaTrader pips. |
| `TakeProfitPips` | `decimal` | `70` | Distancia de obtención de beneficios en MetaTrader pips. |
| `SignalKPeriod` | `int` | `40` | %K período del estocástico de confirmación. |
| `SignalDPeriod` | `int` | `10` | %D suavizado del estocástico de confirmación. |
| `SignalSlowing` | `int` | `10` | Suavizado adicional para el estocástico de confirmación. |
| `EntryKPeriod` | `int` | `40` | %K periodo del estocástico de entrada. |
| `EntryDPeriod` | `int` | `10` | %D suavizado del estocástico de entrada. |
| `EntrySlowing` | `int` | `10` | Suavizado adicional para el estocástico de entrada. |
| `EntryLevel` | `decimal` | `20` | Umbral inferior que confirma el impulso alcista (los cortos usan `100 - EntryLevel`). |
| `FilterKPeriod` | `int` | `40` | Periodo %K del filtro estocástico. |
| `FilterDPeriod` | `int` | `10` | %D suavizado del filtro estocástico. |
| `FilterSlowing` | `int` | `10` | Suavizado adicional para el filtro estocástico. |
| `FilterLevel` | `decimal` | `75` | Umbral superior que limita las configuraciones alcistas (los cortos usan `100 - FilterLevel`). |
| `AcceleratorLevel` | `decimal` | `0.0002` | Amplitud mínima del oscilador del acelerador requerida para las entradas. |
| `AwesomeLevel` | `decimal` | `0.0013` | Impresionante banda osciladora que desencadena salidas comerciales. |

## Diferencias con el experto MetaTrader original
- El puerto StockSharp utiliza suscripciones de velas con enlaces de indicadores en lugar de llamadas repetidas a `CopyBuffer`.
- La gestión de órdenes se realiza en modo posición neta. Cuando el EA se revierte inmediatamente, la conversión primero cierra el
exposición actual y luego emite una nueva orden de mercado en el lado opuesto.
- Las distancias de stop-loss y take-profit se adjuntan a través de `StartProtection`, utilizando cálculos de tamaño de pip derivados del
paso de precio del instrumento. Esto evita modificaciones manuales de los billetes manteniendo las distancias idénticas a los MetaTrader puntos.
- Las solicitudes de volumen están normalizadas según los valores de seguridad `VolumeStep`, `MinVolume` y `MaxVolume` para que el código esté listo para funcionar.
entornos comerciales.

## Consejos de uso
- Ajuste `TradeVolume` para que coincida con el paso de lote mínimo del instrumento antes de ejecutar la estrategia.
- Ajusta los niveles estocásticos (`EntryLevel` y `FilterLevel`) junto con los umbrales del oscilador para adaptar el filtro.
rigor a su mercado.
- Habilite el dibujo de gráficos cuando esté disponible para visualizar los tres osciladores estocásticos, el Oscilador Acelerador, el Impresionante
Oscilador y operaciones ejecutadas.
- Debido a que la lógica espera a que se completen las velas, aparecen señales al cierre de cada barra; utilizar un backtester con el mismo período de tiempo
para obtener resultados consistentes.

## Indicadores
- Tres instancias `StochasticOscillator` con configuraciones de umbral y suavizado independientes.
- `AcceleratorOscillator` para confirmar la entrada.
- `AwesomeOscillator` para el tiempo de salida.
