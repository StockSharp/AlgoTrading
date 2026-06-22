# Estrategia ProMart MACD Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port de StockSharp del experto histórico MQL **MartGreg_1 / ProMart**. Combina dos configuraciones MACD con un modelo de dimensionamiento de posición martingala controlado. El MACD primario busca mínimos y máximos locales en el momentum, mientras que el MACD secundario confirma la dirección de la pendiente reciente. Después de cada operación cerrada, la estrategia sigue el patrón del indicador nuevamente (cuando la última operación fue rentable) o inmediatamente invierte la dirección (tras una pérdida) mientras potencialmente dobla el tamaño de la siguiente orden.

## Lógica de trading

- **Señales**
  - Construir dos indicadores MACD en la serie de velas seleccionada:
    - `MACD1` (rápido=5, lento=20, señal=3) actúa como detector de patrones.
    - `MACD2` (rápido=10, lento=15, señal=3) confirma la pendiente a corto plazo.
  - Evaluar señales solo en velas completadas usando los tres valores MACD1 anteriores y los dos valores MACD2 anteriores (reflejando la lógica MQL que miraba una barra atrás).
  - **Configuración larga**: MACD1 forma un valle local (`MACD1[t-1] > MACD1[t-2] < MACD1[t-3]`) y MACD2 está subiendo (`MACD2[t-2] > MACD2[t-1]`).
  - **Configuración corta**: MACD1 forma un pico local mientras MACD2 está cayendo.
  - Si la última operación cerrada fue rentable, la estrategia espera el próximo setup válido. Tras una operación perdedora abre la dirección opuesta inmediatamente, independientemente de la forma actual del MACD, replicando la reversión martingala original.
- **Gestión de posiciones**
  - Las operaciones se abren con órdenes de mercado y se monitorean en cada vela terminada.
  - Los niveles de stop-loss y take-profit se calculan en puntos de precio desde el precio de entrada. Si el máximo/mínimo de la vela alcanza cualquier nivel, la posición se cierra a mercado y se registra el resultado de la operación.
  - No se abre ninguna nueva operación en la misma vela que cerró una posición; la estrategia espera la siguiente barra, igual que el experto MQL que actuaba en el primer tick de una nueva barra.
- **Dimensionamiento martingala**
  - Un volumen base se deriva del capital del portafolio dividido por `BalanceDivider` y alineado al paso de volumen del instrumento (cayendo de vuelta a la propiedad `Volume` o al volumen mínimo del instrumento cuando es necesario).
  - Después de una operación perdedora, la siguiente posición puede doblar el volumen de la orden anterior, hasta `MaxDoublingCount` veces consecutivas. La rentabilidad reinicia el contador de doblamiento.
  - El volumen siempre está limitado por el volumen máximo del instrumento para evitar el sobredimensionamiento.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `BalanceDivider` | Divisor aplicado al capital del portafolio para calcular el volumen base de la orden. | `1000` |
| `MaxDoublingCount` | Número máximo de doblajes de volumen consecutivos tras pérdidas. | `1` |
| `StopLossPoints` | Distancia del stop-loss medida en puntos de precio (`PriceStep * StopLossPoints`). | `500` |
| `TakeProfitPoints` | Distancia del take-profit medida en puntos de precio. | `1500` |
| `Macd1Fast` / `Macd1Slow` / `Macd1Signal` | Períodos para el MACD primario que detecta valles/picos. | `5 / 20 / 3` |
| `Macd2Fast` / `Macd2Slow` / `Macd2Signal` | Períodos para el filtro de pendiente del MACD secundario. | `10 / 15 / 3` |
| `CandleType` | Tipo de datos de la serie de velas (predeterminado: marco temporal de 1 minuto). | `TimeSpan.FromMinutes(1).TimeFrame()` |

## Notas

- La implementación aproxima los rellenos de stop-loss y take-profit intrabar usando los máximos y mínimos de las velas porque el ejemplo de StockSharp opera en velas terminadas.
- El volumen de la posición cae de vuelta a la propiedad `Volume` de la estrategia o al volumen mínimo del instrumento cuando los datos del portafolio no están disponibles.
- Aún no se proporciona versión Python; solo se incluye la estrategia C#.
- Siempre validar la configuración en datos históricos antes de habilitar el trading real. El componente martingala aumenta significativamente el riesgo.
