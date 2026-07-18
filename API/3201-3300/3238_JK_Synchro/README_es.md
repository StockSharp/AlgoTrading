# Estrategia de JK Synchro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de JK Synchro** es un port de StockSharp del asesor experto de MetaTrader 5 "JK synchro" (MQL ID 2415). El robot original cuenta cuántas de las velas más recientes cerraron por debajo o por encima de su apertura y luego abre una posición en la dirección dominante. Este port replica el comportamiento mientras añade parámetros fuertemente tipados, hooks de gestión de riesgo integrados y registro enriquecido a través de StockSharp.

## Lógica de trading

1. Suscribirse a la fuente de velas definida por `CandleType` y esperar velas terminadas.
2. Mantener una ventana deslizante de `AnalysisPeriod` velas. Para cada vela:
   - Incrementar el contador **bajista** cuando `Open > Close`.
   - Incrementar el contador **alcista** cuando `Open < Close`.
   - Ignorar velas doji donde `Open == Close`.
3. Una vez que la ventana está llena, verificar la dominancia:
   - Si las velas bajistas superan a las alcistas, preparar una entrada **larga**.
   - Si las velas alcistas superan a las bajistas, preparar una entrada **corta**.
4. Antes de entrar en una operación, la estrategia verifica:
   - La estrategia está en línea y tiene permitido operar (`IsFormedAndOnlineAndAllowTrading`).
   - La hora actual está entre `StartHour` y `EndHour` (inclusive).
   - El enfriamiento definido por `PauseBetweenTradesSeconds` ha transcurrido desde la última entrada.
   - Agregar otro lote mantendría la exposición neta dentro de `MaxPositions * OrderVolume`.
5. Cuando aparece una señal mientras se mantiene una posición opuesta, la estrategia primero cierra esa posición y espera la siguiente vela antes de potencialmente entrar en la nueva dirección.
6. Los niveles de stop-loss protector, take-profit y trailing stop se expresan en pips y se traducen automáticamente en offsets de precio basados en el tamaño del tick del instrumento.

## Gestión de riesgo

- **Stop Loss / Take Profit**: Niveles opcionales definidos en pips. Se recalculan en cada cambio de posición y se verifican en cada vela terminada.
- **Trailing Stop**: Activado cuando tanto `TrailingStopPips` como `TrailingStepPips` son positivos. Una vez que la operación se mueve a favor al menos por `TrailingStop + TrailingStep`, el stop sigue el precio usando el paso configurado.
- **Límite de posición**: La posición neta absoluta no puede superar `MaxPositions * OrderVolume`.
- **Pausa de entrada**: La estrategia registra el timestamp de cada ejecución y aplica una pausa antes de abrir otra operación.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `OrderVolume` | 0.1 | Volumen colocado con cada orden de mercado. |
| `MaxPositions` | 10 | Número máximo de lotes permitidos por dirección. |
| `AnalysisPeriod` | 18 | Número de velas terminadas consideradas al contar movimientos alcistas versus bajistas. |
| `PauseBetweenTradesSeconds` | 540 | Enfriamiento en segundos después de cualquier entrada antes de que se pueda abrir una nueva. |
| `StartHour` | 3 | Hora de inicio (inclusive) de la ventana de trading, hora del servidor. |
| `EndHour` | 6 | Hora de fin (inclusive) de la ventana de trading, hora del servidor. |
| `StopLossPips` | 50 | Distancia de stop-loss expresada en pips. Establecer en 0 para deshabilitar. |
| `TakeProfitPips` | 150 | Distancia de take-profit en pips. Establecer en 0 para deshabilitar. |
| `TrailingStopPips` | 15 | Distancia del trailing stop en pips. Establecer en 0 para deshabilitar el trailing. |
| `TrailingStepPips` | 5 | Distancia adicional en pips antes de que se actualice el trailing stop. Debe ser positivo cuando el trailing está habilitado. |
| `CandleType` | Marco temporal de 15 minutos | Fuente de velas usada para todos los cálculos. |

## Notas de implementación

- Se usa la API de alto nivel de StockSharp en todo momento (`SubscribeCandles`, `.Bind`, `BuyMarket`, `SellMarket`).
- Los timestamps de entrada se capturan dentro de `OnPositionChanged` para implementar la lógica de pausa exactamente como el EA original, que esperaba una cantidad fija de tiempo después de cada entrada.
- El tamaño del pip se deriva de `Security.PriceStep` y `Security.Decimals`; para instrumentos de 3 o 5 dígitos el multiplicador se ajusta automáticamente.
- Las salidas se manejan en velas cerradas comparando el máximo/mínimo con los niveles calculados de stop y objetivo.
- Los trailing stops imitan la lógica de MetaTrader: comienzan a moverse solo después de que el beneficio supera `TrailingStop + TrailingStep` y nunca se revierten.

## Consejos de uso

1. Alinear `OrderVolume` y `MaxPositions` con el tamaño del contrato de su broker para mantener la exposición bajo control.
2. Elegir `AnalysisPeriod` según el marco temporal de las velas. Los marcos temporales más cortos generalmente requieren ventanas más grandes para evitar el ruido.
3. Ajustar la ventana de trading para que coincida con las horas activas del instrumento (p. ej., sesión europea para pares basados en EUR).
4. Hacer backtest de diferentes combinaciones de stop, objetivo y configuraciones de trailing: el EA original a menudo se ejecutaba con objetivos fijos o trailing stops dependiendo de las condiciones del mercado.

## Diferencias con la versión MQL

- El port de StockSharp usa un modelo de exposición neta. Al cambiar de dirección, la posición existente se cierra primero, mientras que la versión de MetaTrader podía mantener posiciones cubiertas.
- El registro y la gestión de parámetros aprovechan las instalaciones de StockSharp, haciendo más fácil la optimización y la integración con la interfaz de usuario.
- El trailing stop se evalúa en velas terminadas, lo que es coherente con otros ports de estrategias de StockSharp y evita reaccionar a barras incompletas.

Con estas consideraciones, la estrategia JK Synchro puede ser operada, analizada y optimizada directamente dentro del ecosistema StockSharp.
