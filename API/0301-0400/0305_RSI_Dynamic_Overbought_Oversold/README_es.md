# Estrategia RSI con Sobrecompra/Sobreventa Dinámica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **RSI Dynamic Overbought Oversold** está construida alrededor del RSI con niveles dinámicos de sobrecompra/sobreventa.

Las pruebas indican un retorno anual promedio de aproximadamente el 178%. Funciona mejor en el mercado de acciones.

### Reglas ejecutadas (C# y Python)

Con **velas terminadas de 5 minutos** se calculan RSI(14), SMA(50) del precio y SMA(50) más desviación estándar(50) del propio RSI. Bandas dinámicas: `upper = min(RSI_MA + 2 * RSI_STD, 85)`, `lower = max(RSI_MA - 2 * RSI_STD, 15)`. Estando plano, comprar solo si `RSI <= lower` y `Close >= Price_SMA * 0.995`; vender solo si `RSI >= upper` y `Close <= Price_SMA * 1.005`. Cerrar largo si `RSI >= RSI_MA` o `Close < Price_SMA * 0.995`; corto si `RSI <= RSI_MA` o `Close > Price_SMA * 1.005`. Tras cada orden esperar **48 velas terminadas**. Las posiciones ejecutadas usan un **stop nativo del 2%**, no ATR. La rentabilidad indicada arriba no está verificada por el histórico BTC/TON.



## Detalles
- **Criterios de entrada**: ver implementación para las condiciones del indicador.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `RsiPeriod = 14`
  - `MovingAvgPeriod = 50`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Overbought, Oversold
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
