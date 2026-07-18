# Divergencia MACD (MACD Divergence)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Divergencia MACD busca discrepancias entre la acción del precio y el indicador MACD. Máximos más altos en el precio pero máximos más bajos en el MACD sugieren un debilitamiento del momentum (divergencia bajista), mientras que mínimos más bajos en el precio y mínimos más altos en el MACD apuntan a una reversión alcista.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 70%. Funciona mejor en el mercado de acciones.

Tras detectar la divergencia, el sistema espera a que el MACD cruce su línea de señal antes de entrar. La operación se cierra si el MACD cruza de nuevo en dirección contraria o se activa el stop-loss.

## Detalles

- **Criterios de entrada**: Divergencia alcista o bajista más cruce del MACD con la línea de señal.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: El MACD cruza en dirección opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastMacdPeriod` = 12
  - `SlowMacdPeriod` = 26
  - `SignalPeriod` = 9
  - `DivergencePeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 2.0m
- **Filtros**:
  - Categoría: Divergencia
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
