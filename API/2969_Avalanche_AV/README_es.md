# Estrategia Avalanche AV
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Avalanche AV es una estrategia de martingala aleatorizada que alterna entre entradas largas y cortas con igual probabilidad. Las operaciones se abren solo después de un número configurable de velas terminadas, y cada posición hereda niveles fijos de stop-loss y take-profit definidos en pips. Cuando una operación cierra con pérdida, el tamaño de posición se multiplica por el coeficiente de martingala para perseguir la recuperación; las operaciones rentables restablecen el tamaño al volumen inicial una vez que el saldo de la cuenta registra un nuevo máximo de capital. La estrategia también aplica una caída flotante máxima como porcentaje del saldo de la cuenta y cerrará cualquier posición que supere este umbral.

La versión original MQL abría operaciones en ticks. El port de StockSharp mantiene el mismo comportamiento probabilístico pero trabaja en actualizaciones de velas, haciéndola adecuada tanto para backtesting como para trading en vivo con datos de barras.

## Reglas de trading

- **Intervalo de decisión:** esperar el número especificado de velas terminadas antes de evaluar una nueva señal. Si una posición aún está abierta, el intervalo continúa contando pero no se toma ninguna nueva operación.
- **Dirección de entrada:** generar un número aleatorio; valores por encima de 16384 desencadenan una entrada larga, de lo contrario una entrada corta. Las posiciones se abren solo cuando no hay operación activa.
- **Tamaño de orden:** empezar con `InitialVolume`. Después de cada operación perdedora, el próximo tamaño de orden se convierte en `PreviousVolume * MartingaleMultiplier` (normalizado al paso de volumen del instrumento). Las operaciones ganadoras restablecen el tamaño a `InitialVolume` una vez que el saldo realizado registra un nuevo máximo; de lo contrario la expansión de martingala continúa.
- **Stops y objetivos:** el stop-loss y take-profit se calculan en pips desde el precio de entrada. Un pip es igual al paso de precio del instrumento.
- **Caída flotante:** mientras una posición está activa, la estrategia monitorea el PnL no realizado. Si la pérdida excede `MaxDrawdownPercent` del saldo realizado de la cuenta (`saldo inicial + PnL realizado`), la posición se cierra inmediatamente.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `InitialVolume` | 0.1 | Volumen inicial de operación. |
| `StopLossPips` | 15 | Distancia de stop en pips (0 deshabilita el stop). |
| `TakeProfitPips` | 30 | Distancia de take profit en pips (0 deshabilita el objetivo). |
| `MaxDrawdownPercent` | 75 | Pérdida flotante máxima tolerada como porcentaje del saldo. |
| `MartingaleMultiplier` | 1.6 | Multiplicador de volumen aplicado después de una pérdida. |
| `DecisionInterval` | 9 | Número de velas terminadas entre nuevas decisiones de operación. |
| `CandleType` | Marco temporal de 1 minuto | Tipo de vela que impulsa la estrategia. |

## Notas

- El volumen se normaliza automáticamente a los límites `VolumeStep`, `MinVolume` y `MaxVolume` del instrumento. Si la normalización falla, el tamaño se restablece al volumen inicial.
- Los niveles de stop-loss y take-profit se basan en el `PriceStep` del instrumento como un pip; verifique el paso para símbolos exóticos.
- La protección de caída requiere que tanto `PriceStep` como `StepPrice` estén definidos; de lo contrario, la verificación de seguridad se omite.
- Dado que la estrategia depende de la aleatoriedad, los resultados varían entre ejecuciones incluso con datos de mercado idénticos a menos que la semilla aleatoria se controle externamente.
