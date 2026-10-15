# Macd Pattern Trader v03 (puerto StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Macd Pattern Trader v03 es una estrategia StockSharp de alto nivel convertida del asesor experto MetaTrader 4 *MacdPatternTraderv03*. El robot original busca en la línea principal MACD una formación de reversión de tres picos y aplica reglas de toma de ganancias parciales basadas en promedios móviles. Este puerto de C# conserva la lógica del patrón mientras utiliza StockSharp suscripciones, indicadores y ayudantes de pedidos.

La estrategia está diseñada para configuraciones de agotamiento de tendencias en pares de divisas líquidos, pero se puede aplicar a cualquier instrumento que exponga una curva MACD suave. El plazo predeterminado son velas de 30 minutos, que coinciden con el asesor original, y el tamaño de la operación predeterminado es un contrato (o lote equivalente en términos StockSharp).

## Indicadores y flujo de datos
* **MACD (EMA rápida 5, EMA lenta 13, Señal 1)** — indicador principal utilizado para detectar la estructura de triple techo/triple fondo. La línea de señal no se utiliza; la estrategia se basa únicamente en la línea principal MACD.
* **EMA(7) y EMA(21)**: promedios cortos y medianos utilizados durante la gestión de posiciones.
* **SMA(98) y EMA(365)**: filtros lentos que forman el activador de escalamiento horizontal.

La implementación se suscribe al tipo de vela configurado y vincula los indicadores a través de `Bind`/`BindEx`. Sólo se procesan velas terminadas para evitar actuar sobre datos incompletos.

## Reglas de entrada
### Configuración corta
1. Arma la configuración cuando la línea principal MACD se eleve por encima del nivel de **Activación superior** (predeterminado 0.0030).
2. Registre el primer pico una vez que MACD imprima un máximo local por encima de los valores anteriores y anteriores y luego caiga por debajo del **Umbral superior** (predeterminado 0,0045).
3. Registre el segundo pico si MACD regresa por encima del umbral, alcanza un máximo local más alto y vuelve a caer por debajo del umbral.
4. Confirme el patrón cuando se produzca una tercera reinversión con MACD permaneciendo por debajo del umbral durante tres barras consecutivas y el último máximo local sea inferior al anterior.
5. Si no existe una posición larga, aplane cualquier exposición larga restante y abra una posición corta con el volumen configurado.

### Configuración larga
1. Armar la configuración cuando la línea principal MACD caiga por debajo del nivel de **Activación inferior** (predeterminado −0.0030).
2. Registre el primer valle una vez que MACD imprima un mínimo local por debajo de los dos valores anteriores y luego se eleve por encima del **Umbral inferior** (predeterminado −0,0045).
3. Registre el segundo mínimo si MACD vuelve a caer por debajo del umbral, alcanza un mínimo inferior y vuelve a superar el umbral.
4. Confirme el patrón alcista cuando se observe un tercer repunte con MACD manteniéndose por encima del umbral de tres velas y el último mínimo sea más alto que el anterior.
5. Aplane cualquier exposición corta restante y compre el volumen configurado.

La lógica refleja los indicadores anidados `stops`, `stops1` y `aop_ok*` en el archivo MQ4 original, incluidos los reinicios cada vez que MACD vuelve sobre la banda de activación.

## Gestión comercial
* **Escalamiento horizontal**: cuando las ganancias no realizadas (calculadas como `(Close − Entry) * Position`) exceden `ProfitThreshold` (5 unidades de precio predeterminadas), la estrategia aplica dos salidas por etapas:
  * Etapa 1 (larga): el cierre de la vela anterior debe permanecer por encima de EMA(21). La estrategia vende un tercio de la posición larga inicial. Para las posiciones cortas, el requisito es el cierre anterior por debajo de EMA(21) y se recompra un tercio del volumen corto inicial.
  * Etapa 2 (larga): el máximo de la vela anterior debe atravesar el promedio de SMA(98) y EMA(365). La mitad de la posición larga original está cerrada. Los cortos reflejan esto con el mínimo anterior cayendo por debajo del filtro promedio.
* **Posición residual**: lo que queda después de que este puerto deja la secuencia de escalado sin administrar, coincide con la fuente EA.
* **Órdenes de riesgo**: la versión MetaTrader colocaba órdenes de limitación de pérdidas y toma de ganancias basadas en máximos y mínimos continuos. Debido a que StockSharp administra las órdenes de protección de manera diferente, este puerto no adjunta automáticamente paradas/objetivos. Los usuarios pueden combinar la estrategia con `StartProtection()` o un módulo de riesgo externo si es necesario.

## Parámetros
| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `Volume` | 1 | Tamaño comercial presentado en cada entrada. |
| `CandleType` | plazo de 30 minutos | Serie de velas utilizadas para los cálculos de indicadores. |
| `FastEmaLength` / `SlowEmaLength` | 5 / 13 | MACD períodos rápidos y lentos EMA. |
| `UpperThreshold` / `LowerThreshold` | 0,0045 / −0,0045 | Banda de agotamiento donde ocurren las confirmaciones de patrones. |
| `UpperActivation` / `LowerActivation` | 0,0030 / −0,0030 | Banda exterior que arma las configuraciones bajistas/alcistas. |
| `EmaOneLength` / `EmaTwoLength` | 7 / 21 | EMA auxiliares para visualización y lógica de escalado. |
| `SmaLength` | 98 | Lento SMA usado junto con EMA(365) durante las salidas de la etapa dos. |
| `EmaFourLength` | 365 | EMA a largo plazo utilizado durante las salidas de la etapa dos. |
| `ProfitThreshold` | 5 | PnL mínimo no realizado (precio * unidades de volumen) requerido antes del escalamiento horizontal. |

## Notas practicas
* Asegúrese de que el adaptador del intermediario admita la reducción de posición parcial. El EA original cerraba 1/3 y 1/2 porciones; este puerto replica las mismas fracciones utilizando órdenes de mercado.
* Debido a que las órdenes de protección no se adjuntan automáticamente, considere habilitar `StartProtection()` o agregar reglas de riesgo personalizadas si necesita paradas forzadas.
* El umbral de beneficio se expresa en precio bruto * unidades de volumen. Ajústelo de acuerdo con el tamaño del pip del instrumento o el valor del tick para que coincida con el supuesto de “5 unidades monetarias” del código MQ4 original.
* La estrategia espera una dinámica MACD fluida; El ruido excesivo o los instrumentos ilíquidos pueden impedir que se active la lógica de tres picos.

## Diferencias con la versión MQ4
* Utiliza enlaces de indicadores StockSharp en lugar de llamadas repetidas a `iMACD`.
* El cálculo de ganancias no realizadas se basa en `Position` y `PositionAvgPrice`, lo que significa que las reglas de redondeo del corredor pueden diferir de las `OrderProfit()`.
* Las órdenes de stop-loss y take-profit no se generan automáticamente; Si es necesario, se deben agregar herramientas de riesgo manuales.
* El parámetro MQ4 `sum_bars_bup` no está presente porque no se usó en la fuente original.
