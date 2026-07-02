# Estrategia de muestra de TradePad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de muestra de TradePad** es una adaptación del ejemplo MetaTrader "TradePad". El asesor experto original presentó una cuadrícula de
Botones que mostraban la tendencia a corto plazo para múltiples símbolos coloreando cada celda con el oscilador Stochastic actual.
leyendo. Esta implementación StockSharp mantiene el núcleo analítico de la muestra y se centra en monitorear una lista de instrumentos.
sin replicar la interfaz de usuario en el gráfico. La estrategia se suscribe a los datos de velas para cada símbolo configurado, calcula un
Stochastic oscilador y clasifica cada instrumento en estados *Tendencia alcista*, *Tendencia bajista* o *Plano*. Cada vez que cambia la clase,
la estrategia escribe un mensaje de registro similar al cambio de color realizado por el TradePad original.

La estrategia no realiza pedidos. Su objetivo es ayudar a los operadores discrecionales a realizar un seguimiento de varios mercados a la vez y detectar
cambios de impulso que requieren acciones manuales (por ejemplo, cambiar de gráfico o preparar operaciones).

## Cómo funciona

1. **Descubrimiento de símbolos**: el parámetro `SymbolList` acepta una lista de tickers separados por comas. Si no se proporciona ninguna lista, el
La estrategia vuelve al `Security` principal asignado en el corredor.
2. **Suscripción de vela**: cada símbolo utiliza el mismo período de tiempo configurado a través de `CandleType`.
3. **Procesamiento de indicadores**: una instancia `StochasticOscillator` dedicada está vinculada al flujo de velas. Cuando la vela está
terminado, el indicador produce el valor `%K` utilizado para la clasificación de tendencias.
4. **Clasificación de tendencias**: una lectura por encima de `UpperLevel` se asigna a *Tendencia alcista*, una lectura por debajo de `LowerLevel` se asigna a *Tendencia a la baja*,
todo lo que hay en el medio es *Plano*. El último valor del oscilador se almacena en `LatestKValues`.
5. **Intervalo de actualización**: la estrategia imita el comportamiento del temporizador del TradePad original. Un cambio se registra como máximo una vez por
`TimerPeriodSeconds` para cada símbolo incluso si llegan varias velas dentro del intervalo.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `SymbolList` | Lista separada por comas de instrumentos para monitorear. Una cadena vacía significa "usar la seguridad principal". |
| `TimerPeriodSeconds` | Número mínimo de segundos entre actualizaciones de estado por símbolo. Evita el spam de registros cuando las velas son muy cortas. |
| `StochasticLength` | Período retrospectivo utilizado para calcular la línea `%K` sin procesar. |
| `StochasticKPeriod` | Período de suavizado aplicado a la línea `%K`. |
| `StochasticDPeriod` | Período de suavizado aplicado a la línea `%D` (se mantiene para que esté completo, aunque la estrategia solo dice `%K`). |
| `UpperLevel` | Umbral por encima del cual se considera que el símbolo está en tendencia alcista. |
| `LowerLevel` | Umbral por debajo del cual se considera que el símbolo está en tendencia bajista. |
| `CandleType` | Periodo de tiempo de las velas utilizadas para el cálculo del indicador. |

## Notas de uso

- Asegúrese de que los tickers especificados estén disponibles en el conector; Los símbolos faltantes se informan en el registro y se omiten.
- La propiedad `TrendStates` expone la clasificación más reciente para paneles externos o bloques de Designer.
- Utilice la estrategia dentro de Designer o Runner para adjuntar sus propios elementos visuales (paneles, gráficos) que reaccionen al `AddInfoLog`
mensajes o los diccionarios públicos.
- Debido a que no se envían órdenes, es seguro ejecutar la estrategia en proveedores de datos en vivo únicamente con fines de monitoreo.

## Comportamiento original de MQL frente a la versión StockSharp

| MQL5 Característica | StockSharp Implementación |
|--------------|--------------------------|
| Cuadrícula gráfica de botones. | Expuestos como entradas de registro y diccionarios públicos para que se pueda crear una interfaz de usuario personalizada en Designer. |
| Botones manuales de COMPRAR/VENDER | No implementado; la estrategia permanece intencionalmente pasiva. |
| Lógica de arrastre de gráficos | No aplicable en StockSharp y omitido. |
| Actualizaciones de colores de tendencia | Reemplazado con cambios de estado de tendencia activados cada `TimerPeriodSeconds` por símbolo. |

## Ampliando la estrategia

- Conecte el diccionario `TrendStates` a los widgets de Designer para reconstruir el panel de color mediante controles XAML.
- Agregue alertas o notificaciones cuando un símbolo pase de *Plano* a *Tendencia alcista* o *Tendencia bajista*.
- Combine la clasificación con la lógica de orden si desea automatizar las entradas después de identificar un fuerte impulso.
