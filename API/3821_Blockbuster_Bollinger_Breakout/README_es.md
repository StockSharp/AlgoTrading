# Estrategia de ruptura de Blockbuster Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Breakout de Blockbuster Bollinger es una adaptación directa del asesor experto MetaTrader 4 "BLOCKBUSTER EA". El sistema original buscó reversiones agresivas después de que el precio superó una banda Bollinger en una distancia configurable. Esta versión StockSharp mantiene la misma lógica al tiempo que adopta el API de alto nivel para suscripciones de velas, vinculación de indicadores y gestión de posiciones.

## Idea central

1. Cree Bollinger bandas con un período y una desviación definidos por el usuario.
2. Mida cuando el cierre de la vela actual se rompe por encima de la banda superior o por debajo de la banda inferior en un desplazamiento adicional (en puntos).
3. Ingrese corto si el cierre excede la banda superior más el desplazamiento. Entre en largo si el cierre cae por debajo de la banda inferior menos el desplazamiento.
4. Administre la posición con umbrales de pérdidas y ganancias basados en puntos idénticos a la configuración de MQL.

La distancia, la parada y el objetivo se expresan en puntos del instrumento. Se adaptan al paso del precio del instrumento, por lo que un valor de `3` significa tres `PriceStep` unidades independientemente del símbolo subyacente.

## Lógica detallada

- **Cálculo del indicador**
  - Indicador: Bollinger Bandas.
  - Entradas: precios de cierre de velas (el código MT4 usó `PRICE_OPEN`; este puerto mantiene los precios de cierre para una mejor compatibilidad StockSharp al tiempo que preserva la longitud de la banda y los parámetros de desviación).
  - Parámetros:
    - `BollingerPeriod`: número de velas utilizadas en la media móvil y la desviación estándar.
    - `BollingerDeviation`: multiplicador de desviación estándar para las bandas superior e inferior.
  - Compensación adicional `DistancePoints` (convertida a precio utilizando el instrumento `PriceStep`).

- **Condiciones de entrada**
  - **Long**: `Close < LowerBand - Distance` y la posición neta actual es plana o corta.
  - **Corto**: `Close > UpperBand + Distance` y la posición neta actual es plana o larga.
  - Cualquier posición opuesta abierta se aplana según el tamaño de la orden de mercado `TradeVolume + |Position|` para reflejar el comportamiento de "una sola orden" de MT4.

- **Condiciones de salida**
  - Las posiciones se controlan en cada vela terminada. El beneficio no realizado en puntos se calcula utilizando el instrumento `PriceStep`.
  - **Take Profit**: si la ganancia alcanza o excede `ProfitTargetPoints`.
  - **Stop Loss**: si la pérdida alcanza o supera `LossLimitPoints`.
  - Las salidas se realizan con órdenes de mercado que cierran la posición completa.

- **Gestión de riesgos y dinero**
  - `TradeVolume` establece el tamaño del pedido base. Hacer coincidir la entrada "Lotes" MetaTrader es tan simple como establecer el mismo valor numérico.
  - Tanto la parada como el objetivo se pueden desactivar configurando el parámetro respectivo en `0`.
  - Cuando ambos umbrales están habilitados, la parada se evalúa después del objetivo, exactamente como el EA original verificó primero la rama de ganancias.

- **Seguimiento del estado**
  - La estrategia registra el precio de entrada en el momento de la señal y lo utiliza para todos los cálculos de pérdidas y ganancias posteriores.
  - Si una orden de salida aplana la posición, el estado se restablece automáticamente.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `BollingerPeriod` | 20 | Número de velas en la media móvil de Bollinger bandas. |
| `BollingerDeviation` | 2.0 | Multiplicador de desviación estándar. |
| `DistancePoints` | 3 | Distancia adicional más allá de la banda antes de realizar una operación (puntos de instrumento). |
| `ProfitTargetPoints` | 3 | Umbral de toma de ganancias en puntos del instrumento. Establezca en 0 para desactivar. |
| `LossLimitPoints` | 20 | Umbral de stop-loss en puntos del instrumento. Establezca en 0 para desactivar. |
| `TradeVolume` | 1 | Volumen para nuevas entradas. |
| `CandleType` | marco de tiempo de 1 minuto | Tipo de vela utilizada para los cálculos. |

## Notas de uso

- Funciona con cualquier instrumento que suministre velas y un `PriceStep` distinto de cero. Los pares de divisas, los CFD sobre índices y los futuros líquidos reflejan mejor el entorno original de EA.
- Debido a que el indicador ahora se basa en los precios de cierre, se recomienda realizar pruebas en el período de tiempo previsto para garantizar un comportamiento similar al de la versión MT4.
- La estrategia utiliza `CreateChartArea` ayudantes para visualizar velas, las Bollinger bandas y operaciones ejecutadas cuando hay un gráfico disponible en la interfaz de usuario.
- La lógica supone una evaluación continua de las velas terminadas, asegurando un comportamiento determinista en el backtesting y el trading en vivo.

## Etiquetas

- Categoría: Ruptura contratendencia
- Dirección: Ambos
- Indicadores: Bollinger Bandas
- Paradas: Sí (configurable)
- Plazo: Corto plazo (predeterminado 1 minuto)
- Complejidad: Simple
