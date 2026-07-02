# La mejor estrategia MACD de TradingLab
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto de MetaTrader "TradingLab_Best_MACD_Strategy" utilizando el API de alto nivel de StockSharp. Combina una estructura de media móvil, cruces MACD y comprobaciones dinámicas de soporte/resistencia para abrir operaciones direccionales que se alinean con el impulso y las reacciones recientes de los precios.

## Lógica principal

- **Fuente de velas**: utiliza el parámetro configurable `CandleType` para suscribirse a velas terminadas. Sólo las velas completadas generan decisiones comerciales.
- **Filtro de tendencias**: una media móvil simple de 200 períodos define la tendencia predominante. Las operaciones largas requieren que el cierre se mantenga por encima del promedio, mientras que las operaciones cortas requieren que el cierre se mantenga por debajo de él.
- **Cuadro de soporte y resistencia**: una ventana más alta/más baja de 20 períodos emula el indicador personalizado "Cuadro". Tocar la resistencia o el nivel de soporte anterior genera configuraciones cortas o largas para un número limitado de velas controladas por `SignalValidity`.
- **MACD Cruces**: un MACD estándar (12, 26, 9 por defecto) debe cruzar su línea de señal en la vela anterior y permanecer en el lado requerido de la línea cero. Cada cruce válido mantiene viva su señal durante `SignalValidity` velas, reflejando la lógica de cuenta regresiva de la fuente EA.
- **Momento de entrada**: se abre una posición cuando tanto el MACD como el toque de soporte/resistencia correspondiente siguen siendo válidos, y al menos uno de ellos se activa en la vela actual.
- **Lógica de salida**: al entrar, los objetivos dinámicos de stop-loss y take-profit se calculan en relación con la distancia promedio móvil. La distancia de obtención de beneficios es `RiskRewardMultiplier` veces la distancia ajustada utilizada para la parada. Las salidas protectoras monitorean las velas posteriores y llaman a `ClosePosition()` una vez que el precio supera los niveles almacenados.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` | Volumen fijo enviado con cada orden de mercado. |
| `SignalValidity` | Número de velas que mantienen activos MACD y activadores de soporte/resistencia. |
| `MaLength` | Período del filtro de tendencia de media móvil simple. |
| `BoxPeriod` | Longitud retrospectiva del cuadro más alto/más bajo que rastrea la resistencia y el soporte recientes. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD períodos rápido, lento y de señal. |
| `StopDistancePoints` | Distancia desde la media móvil hasta el stop-loss, expresada en puntos estilo MetaTrader (multiplicados por el paso del precio del símbolo). |
| `RiskRewardMultiplier` | Multiplicador aplicado a la distancia MA ajustada para producir el objetivo de obtención de beneficios. |
| `CandleType` | Tipo de datos que describe la serie de velas a la que suscribirse (predeterminado: período de 1 hora). |

## Notas

- La detección de soporte y resistencia sigue la idea original al observar si la vela anterior rompe los niveles más alto/más bajo de los 20 períodos. Cada toque reinicia los contadores de validez.
- Las paradas y los objetivos se vuelven a calcular para cada nueva entrada y se comparan con el máximo/mínimo de cada vela terminada para imitar el monitoreo intrabar de MetaTrader de manera determinista.
- La gestión protectora se basa en el instrumento `PriceStep`. Si un instrumento informa un paso cero, se utiliza un retroceso de 0,0001.
