# Estrategia Awesome Oscillator Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Awesome Oscillator Trader es una conversión directa del asesor experto de MetaTrader "AwesomeOscTrader". Combina el Awesome Oscillator de Bill Williams con filtros de anchura de bandas de Bollinger y oscilador Estocástico para temporizar rupturas después de contracciones profundas de momentum. El sistema está diseñado para negociación horaria de un solo símbolo en pares FX muy líquidos como EURUSD, reflejando la recomendación original.

La estrategia espera a que el spread de las bandas de Bollinger entre en un rango configurable, señalando que la volatilidad se ha contraído sin desaparecer. Durante esa compresión, el histograma del Awesome Oscillator debe imprimir un patrón distintivo de reversión de cinco barras: cuatro barras consecutivas descendentes que permanecen bajo cero, seguidas de una nueva barra que cambia al color alcista mientras sigue negativa. Cuando esta estructura se forma y el Estocástico cruza de vuelta por encima de un nivel de sobreventa, la estrategia abre una posición larga esperando que la compresión se resuelva al alza. El patrón inverso — cuatro barras positivas ascendentes sobre cero y una nueva barra de color bajista que sigue positiva — combinado con el Estocástico cayendo por debajo de un umbral superior, dispara una entrada corta.

Las posiciones se protegen con una distancia de stop basada en ATR. En cada barra el sistema lee el Average True Range de 3 periodos, lo multiplica por un factor configurable y convierte el resultado a pips según el tamaño de tick del instrumento. Ese valor define tanto el stop-loss inicial como los objetivos de take-profit, reproduciendo la lógica de salida simétrica de MetaTrader. Un trailing stop opcional ajusta el nivel protector cuando el precio se mueve a favor por el número configurado de pips, mientras que `CloseOnReversal` cierra posiciones cuando aparece el patrón opuesto del Awesome Oscillator o un cambio de color. Un filtro de beneficio permite cerrar solo operaciones ganadoras, solo perdedoras o todas ante señales de reversión, replicando el comportamiento `ProfitTypeClTrd` del EA.

## Reglas de negociación

- **Marco temporal:** velas de 1 hora por defecto (totalmente configurable).
- **Filtros:**
  - La anchura de las bandas de Bollinger debe estar entre `BollingerSpreadLower` y `BollingerSpreadUpper` pips.
  - Stochastic %K se compara con `StochasticLowerLevel` para largos y `StochasticUpperLevel` para cortos.
  - Awesome Oscillator debe construir la estructura de reversión de cinco barras, con la barra más reciente cambiando de color mientras permanece en el lado opuesto de cero, y su magnitud normalizada debe superar `AoStrengthLimit`.
- **Entradas:**
  - **Largo:** condiciones anteriores más que la barra actual esté dentro de la ventana horaria permitida.
  - **Corto:** condiciones reflejadas.
- **Salidas:**
  - Niveles de stop-loss y take-profit derivados de ATR fijados simétricamente en la entrada.
  - Trailing stop (si `TrailingStopPips` &gt; 0) se ajusta en dirección del beneficio.
  - Cierre opcional ante señal opuesta o cambio de color del oscilador según `CloseOnReversal` y `ProfitFilter`.

## Parámetros clave

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | 1 hora | Marco usado para todos los indicadores. |
| `BollingerPeriod` | 20 | Periodo del filtro de volatilidad de bandas de Bollinger. |
| `BollingerSigma` | 2.0 | Multiplicador de desviación estándar para las bandas de Bollinger. |
| `BollingerSpreadLower` | 24 pips | Spread mínimo de bandas requerido para operar. |
| `BollingerSpreadUpper` | 230 pips | Spread máximo de bandas permitido. |
| `AoFastPeriod` / `AoSlowPeriod` | 4 / 28 | Periodos rápido y lento del Awesome Oscillator. |
| `AoStrengthLimit` | 0.0 | Magnitud AO normalizada mínima para confirmar entradas. |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | 1 / 4 / 1 | Longitudes del oscilador Estocástico que reproducen los valores de MetaTrader. |
| `StochasticLowerLevel` / `StochasticUpperLevel` | 12 / 21 | Umbrales de sobreventa y sobrecompra para confirmar señales. |
| `EntryHour` / `OpenHours` | 16 / 13 | Hora de inicio y duración de la ventana de negociación. Maneja el cruce de medianoche como el EA. |
| `RiskPercent` | 0.5% | Porcentaje de riesgo usado para dimensionamiento cuando hay datos de cuenta. |
| `AtrMultiplier` | 4.5 | Multiplicador aplicado al ATR de 3 periodos para calcular la distancia de stop. |
| `TrailingStopPips` | 40 pips | Distancia del trailing stop opcional (0 para desactivar). |
| `ProfitFilter` | OnlyProfitable | Selecciona si las salidas por reversión pueden cerrar cualquier operación, solo rentables o solo perdedoras. |
| `MaxOpenOrders` | 1 | Número máximo de posiciones simultáneas (se mantiene en 1 para coincidir con el EA). |

## Notas de implementación

- Usa indicadores StockSharp `BollingerBands`, `StochasticOscillator`, `AwesomeOscillator`, `AverageTrueRange` y `Highest`; no hay cálculos manuales de indicadores.
- Los valores AO se normalizan sobre las últimas 100 barras para imitar los búferes del indicador MetaTrader y reproducir la lógica de color sin código personalizado.
- El dimensionamiento respeta `Security.StepVolume`, `Security.MinVolume`, `Security.MaxVolume` y `Security.StepPrice` cuando están disponibles; si no, usa el volumen predeterminado de la estrategia.
- Los niveles protectores se gestionan íntegramente dentro de la estrategia: las comprobaciones de stop y take-profit se ejecutan en cada vela terminada, igualando la gestión por tick del EA sin necesitar órdenes del broker.
- Todos los comentarios del código están en inglés y la indentación usa tabulaciones según las directrices del proyecto.
