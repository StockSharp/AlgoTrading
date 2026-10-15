# Estrategia iCCI iMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto MetaTrader «iCCI iMA». Opera cruces del Commodity Channel Index (CCI) contra una media móvil exponencial (EMA) aplicada directamente al flujo del CCI. Un CCI secundario, calculado con su propio período, supervisa reversiones de sobrecompra/sobreventa alrededor de las bandas ±100. Las órdenes se dimensionan en lotes, opcionalmente escaladas por el balance de la cuenta, y cada operación está protegida por niveles configurables de stop-loss y take-profit expresados en pips.

## Cómo funciona
* **Fuente de datos** – Una serie de velas configurable (velas de 1 minuto por defecto) alimenta todos los cálculos de indicadores usando el precio típico de la vela `(high + low + close) / 3`.
* **Indicadores principales** – El CCI primario mide el momentum con la longitud `CciPeriod`. Una EMA de ese CCI (longitud `MaPeriod`) suaviza el oscilador y actúa como línea de señal. El CCI secundario `CciClosePeriod` monitorea cruces de umbral.
* **Lógica de entrada** – Una posición larga se abre cuando el CCI actual está por encima de su EMA mientras el valor de hace dos velas completadas estaba por debajo de la EMA, indicando un cruce ascendente. Una posición corta refleja esta lógica cuando el CCI cruza hacia abajo. El algoritmo solo opera después de que todos los indicadores estén completamente formados y dos barras históricas estén disponibles para reproducir el look-back original de la implementación MQL.
* **Lógica de salida** – Los largos existentes se cierran cuando el CCI secundario cae de vuelta por debajo de +100 o cuando el CCI primario cae bajo la EMA después de haber estado por encima dos barras antes. Los cortos salen cuando el CCI secundario sube por encima de −100 o cuando el CCI vuelve a subir por encima de la EMA bajo la misma confirmación de dos barras. Los stops protectores monitorean cada vela finalizada: las posiciones largas se cierran si el precio baja a `entry − stopLossPips * pipSize` y toman beneficios en `entry + takeProfitPips * pipSize`; los cortos usan los niveles simétricos con `entry + stopLoss` y `entry − takeProfit`. El tamaño de pip se deriva del paso de precio del valor y se adapta a cotizaciones de 3 o 5 dígitos multiplicando el tamaño del tick por 10, coincidiendo con la conversión de MetaTrader.
* **Dimensionamiento de posición** – El tamaño de lote base (`LotSize`) se valida contra los valores `VolumeStep`, `MinVolume` y `MaxVolume` del instrumento para que las órdenes respeten las restricciones del exchange. Si la gestión de dinero está habilitada, la estrategia multiplica el tamaño de lote por un factor entero igual al balance de la cuenta dividido por `DepositPerLot`, limitado a 20, y actualizado en cada barra, reproduciendo el escalado entero del experto original.

## Parámetros
- **Tipo de Vela** – Serie de datos usada para los cálculos de indicadores.
- **Período CCI** – Longitud del CCI primario que impulsa las señales de cruce.
- **Período CCI Cierre** – Longitud del CCI secundario usado para vigilar las reversiones ±100.
- **Período EMA CCI** – Período de la EMA que suaviza los valores del CCI primario.
- **Tamaño de Lote** – Volumen de trading base en lotes antes de cualquier escalado.
- **Habilitar Gestión de Dinero** – Activa el escalado del tamaño de lote basado en balance.
- **Depósito Por Lote** – Incremento de balance requerido para aumentar el multiplicador de lote en uno (activo solo cuando la gestión de dinero está activada).
- **Stop Loss (pips)** – Distancia de stop protector en pips; establecer en cero para deshabilitar.
- **Take Profit (pips)** – Distancia del objetivo de beneficio en pips; establecer en cero para deshabilitar.

El algoritmo requiere dos velas completamente terminadas antes de comenzar a operar para que las comparaciones de cruce de dos barras coincidan con la lógica MQL fuente. Las verificaciones de stop-loss y take-profit se evalúan en velas cerradas usando sus extremos de máximo/mínimo, lo que aproxima las órdenes protectoras del lado del servidor de MetaTrader dentro de la API StockSharp de alto nivel.
