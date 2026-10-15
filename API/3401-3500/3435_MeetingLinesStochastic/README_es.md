# Estrategia de líneas de reunión Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de Líneas de Reunión Stochastic** es una implementación StockSharp del experto MetaTrader *Expert_AML_Stoch*. Combina los patrones de inversión de velas alcistas/bajistas con la confirmación de la línea de señal %D del oscilador Stochastic. La estrategia está diseñada para traders discrecionales que desean un enfoque basado en reglas para el reconocimiento de patrones con confirmación de impulso adicional. Al utilizar StockSharp API de alto nivel, el código sigue siendo conciso, comprobable y fácil de ampliar para la gestión de cartera o una mayor automatización.

## Lógica de trading

1. **Filtro de patrón de velas**
   - La estrategia evalúa continuamente las dos últimas velas completadas para detectar una formación de Líneas de Encuentro.
   - Una configuración alcista requiere una vela negra larga seguida de una vela blanca larga cuyo precio de cierre esté dentro del 10% del cierre anterior.
   - Una configuración bajista requiere una vela blanca larga seguida de una vela negra larga con la misma alineación cercana del 10%.
   - El tamaño medio del cuerpo de la vela se calcula con una media móvil simple configurable para filtrar los cuerpos débiles.

2. **Stochastic Confirmación**
   - La línea de señal %D del oscilador Stochastic debe confirmar la señal de la vela.
   - Las entradas alcistas exigen que %D esté por debajo del umbral de sobreventa configurable (predeterminado 30).
   - Las entradas bajistas requieren que %D esté por encima del umbral de sobrecompra configurable (predeterminado 70).

3. **Reglas de salida**
   - Las posiciones cortas se cierran cuando %D cruza hacia arriba a través del nivel de salida inferior (predeterminado 20) o el nivel de salida superior (predeterminado 80).
   - Las posiciones largas se cierran cuando %D cruza hacia abajo por los mismos niveles.
   - Las órdenes de reversión cierran automáticamente la exposición existente y abren una nueva posición en la dirección opuesta.

4. **Manejo de volumen**
   - La estrategia utiliza la propiedad base `Volume` cuando es positiva; de lo contrario, el valor predeterminado es un lote único por compatibilidad con el comportamiento de lote fijo de MetaTrader.

## Parámetros

| Nombre | Descripción | Predeterminado | Notas |
| ---- | ----------- | ------- | ----- |
| `CandleType` | Serie de velas primarias utilizadas para el análisis. | plazo de 15 minutos | Acepta cualquier `DataType` compatible con StockSharp. |
| `StochasticLength` | Período retrospectivo para el cálculo de %K sin procesar. | 3 | Refleja el MetaTrader `%K period`. |
| `StochasticSmoothing` | Suavizado aplicado a %K (MetaTrader `slowing`). | 25 | Establece la longitud de suavizado interno del oscilador. |
| `StochasticSignal` | Período de suavizado para la línea de señal %D. | 36 | Refleja el MetaTrader `%D period`. |
| `BodyAveragePeriod` | Número de velas utilizadas para promediar el tamaño del cuerpo de la vela. | 3 | Filtra cuerpos menores al detectar líneas de encuentro. |
| `LongEntryLevel` | Valor máximo de %D que aún permite una entrada alcista. | 30 | Equivale al umbral de sobreventa. |
| `ShortEntryLevel` | Valor mínimo de %D requerido para una entrada bajista. | 70 | Equivale al umbral de sobrecompra. |
| `ExitLowerLevel` | Límite inferior que desencadena salidas en cruces alcistas. | 20 | Se utiliza para decisiones de salida tanto largas como cortas. |
| `ExitUpperLevel` | Límite superior que desencadena salidas en cruces descendentes. | 80 | Se utiliza para decisiones de salida tanto largas como cortas. |

Todos los parámetros se exponen a través de `StrategyParam<T>` y se pueden optimizar directamente en StockSharp Designer o mediante programación.

## Generación de señal

- **Entrada larga**: Líneas de reunión alcistas + %D por debajo de `LongEntryLevel` sin exposición larga existente (las posiciones cortas se revierten).
- **Entrada corta**: Líneas de encuentro bajistas + %D por encima de `ShortEntryLevel` sin exposición corta existente (las posiciones largas se invierten).
- **Salida larga**: %D cruza por debajo de `ExitUpperLevel` o `ExitLowerLevel`.
- **Salida corta**: %D cruza por encima de `ExitLowerLevel` o `ExitUpperLevel`.

## Notas de implementación

- Los datos de los indicadores se manejan a través de `BindEx`, evitando la gestión manual de recopilación de indicadores.
- El promedio del cuerpo de la vela utiliza un `SimpleMovingAverage` alimentado con tamaños de cuerpo absolutos hasta el `DecimalIndicatorValue`, que coincide con el MetaTrader auxiliar `AvgBody`.
- Todos los comentarios dentro del código están escritos en inglés y la sangría se basa en caracteres de tabulación de acuerdo con las pautas del proyecto.
- La estrategia dibuja automáticamente velas y el oscilador estocástico cuando hay un área del gráfico disponible, lo que simplifica el monitoreo en vivo.

## Consejos de uso

1. **Optimización**: Utilice los parámetros expuestos para las pruebas de avance para alinear los umbrales con el instrumento negociado.
2. **Gestión de riesgos**: superponga la estrategia con los controles de riesgo integrados `StartProtection` o externos a nivel de cartera de StockSharp para implementaciones de producción.
3. **Calidad de los datos**: Los patrones de las líneas de reunión son sensibles a los precios precisos de apertura y cierre; garantizar la alineación del feed y el filtrado de sesiones sin liquidez.
4. **Marcos de tiempo**: aunque el valor predeterminado es 15 minutos, se pueden usar datos intradiarios o diarios modificando `CandleType`.

La estrategia ofrece un enfoque disciplinado para los operadores que dependen de formaciones de velas pero requieren la confirmación del oscilador para reducir los falsos positivos.
