# Estrategia de interfaz de usuario de Sudoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una adaptación StockSharp del script MetaTrader 5 **SudokuUI.mq5**. El programa original MQL expone una interfaz gráfica de Sudoku con parámetros que controlan la generación, mezcla y actualizaciones automáticas de los rompecabezas. Debido a que el entorno StockSharp se centra en el comercio automatizado en lugar de widgets de gráficos interactivos, el puerto reutiliza los conceptos subyacentes en una estrategia de reversión de la media basada en cuadrículas impulsada por estadísticas de rompecabezas.

El tablero de Sudoku se interpreta como una matriz de dígitos de 9x9. Los promedios de columnas definen umbrales de desviación simétrica en torno a una media móvil simple (SMA). Cuando el precio se desvía del SMA más allá de estos niveles derivados del Sudoku, la estrategia entra en una posición en la dirección opuesta, buscando una reversión hacia la media. Regresar a una zona neutral cierra la posición, imitando la capacidad de la herramienta original para restablecer el tablero.

## Lógica de trading

1. **Preparación de rompecabezas**
   - La estrategia puede cargar una especificación de Sudoku de 81 dígitos desde un archivo o una cadena sin formato. Los caracteres que no son dígitos se ignoran y se omiten los ceros, lo que cumple con los requisitos de dígitos del Sudoku.
   - Cuando no se proporciona ningún rompecabezas válido, se genera un tablero pseudoaleatorio al barajar repetidamente grupos de dígitos. La lógica respeta tanto las semillas de *barajado* como de *composición* que fueron expuestas en la versión MQL para que los comerciantes puedan obtener diseños reproducibles.
   - Se puede eliminar un dígito específico antes de calcular las estadísticas. Esto imita la opción GUI original que ocultaba ciertas etiquetas y proporciona una manera fácil de reducir la cuadrícula activa.

2. **Construcción de niveles**
   - Cada columna del rompecabezas se promedia después del paso de eliminación. El promedio se normaliza al rango [-1, 1] y se multiplica por `ThresholdRange`, lo que produce niveles de desviación de precios expresados ​​como fracciones del valor de SMA.
   - Se insertan niveles de respaldo negativos o positivos si el rompecabezas solo produce valores en un lado del SMA, lo que garantiza que existan activadores largos y cortos.

3. **Generación de señal**
   - La estrategia se suscribe al tipo de vela configurado y lo vincula a un indicador SMA. Solo se procesan velas terminadas, siguiendo las mejores prácticas de StockSharp.
   - Cuando la distancia porcentual entre el precio de cierre y el SMA cruza por debajo del nivel más negativo, se abre una posición larga (después de aplanar las ventas cortas). Cruzar por encima del nivel positivo más alto abre una posición corta de la misma manera.
   - Una banda neutral alrededor de la desviación cero (`NeutralBand`) fuerza una exposición plana. Esto reemplaza al "asistente" de Sudoku que ajustaba automáticamente el estado del rompecabezas.

4. **Actualización automática**
   - Establecer `EnableAutoUpdate` en `true` hace que la cuadrícula de Sudoku se regenere al comienzo de cada día de negociación. La mezcla de semillas, la configuración de eliminación y el conteo aleatorio influyen en los umbrales recalculados, proporcionando una cuadrícula dinámica pero reproducible.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `PuzzleDefinition` | Ruta de archivo o dígitos en línea que describen el Sudoku utilizado para los cálculos de niveles. |
| `ShufflingRandomSeed` | Semilla primaria para la generación de rompecabezas. `-1` deriva la semilla del día de negociación. |
| `CompositionRandomSeed` | Semilla secundaria que perturba el proceso de barajado para crear diseños alternativos. |
| `ShufflingCycles` | Número de pases de barajado adicionales aplicados al grupo de dígitos. Los valores más altos crean tableros más aleatorios. |
| `EliminateLabel` | Dígito (1-9) eliminado del tablero antes de calcular los promedios. `0` mantiene todos los dígitos. |
| `EnableAutoUpdate` | Reconstruye los niveles del rompecabezas cuando cambie la fecha de negociación. |
| `SmaPeriod` | Longitud del indicador SMA utilizado como ancla de reversión. |
| `ThresholdRange` | Desviación absoluta máxima (expresada como fracción del precio) producida por el rompecabezas. |
| `NeutralBand` | Zona de desviación que provoca el aplanamiento de la posición cuando el precio vuelve a entrar en ella. |
| `Volume` | Volumen de pedidos para entradas al mercado. |
| `CandleType` | Suscripción de vela utilizada para actualizaciones de indicadores. |

## Notas de uso

- La estrategia solo reacciona a velas completamente formadas e ignora los precios cero, lo que garantiza un comportamiento estable entre los proveedores de datos.
- Proporcione una cadena de dígitos de 81 caracteres (sin ceros) o un archivo de texto que contenga dichos dígitos para reproducir exactamente un tablero de Sudoku de la versión MetaTrader.
- Si necesita una cuadrícula estacionaria, desactive `EnableAutoUpdate` y establezca semillas explícitas. Habilitar la opción refleja el MQL "asistente automático" que mantuvo el tablero sincronizado con las acciones del usuario.
- Los umbrales se derivan de las estadísticas de las columnas. Para acertijos asimétricos, considere eliminar el dígito dominante para mantener una cobertura equilibrada de compra/venta.

## Diferencias con el guión original

- Se eliminan todas las funciones de la interfaz de usuario (ventanas de diálogo, botones, eventos de gráficos). Sus equivalentes funcionales se exponen como parámetros de estrategia.
- En lugar de resolver Sudokus manualmente, el tablero influye en los niveles de negociación algorítmica. Los mismos controles de aleatoriedad determinan qué tan agresivos o conservadores se vuelven esos niveles.
- La versión StockSharp se ejecuta de forma autónoma. La actualización automática ahora reacciona a los días de negociación en lugar de a los clics en los botones, y la gestión de posiciones se realiza mediante llamadas estándar `BuyMarket`/`SellMarket`/`ClosePosition`.
