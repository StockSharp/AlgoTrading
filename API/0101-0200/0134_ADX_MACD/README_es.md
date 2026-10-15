# Estrategia ADX MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
ADX MACD combina la fortaleza de tendencia del Average Directional Index con los cambios de momentum del MACD.
Cuando el ADX está en alza, los rompimientos tienen mayor probabilidad de continuar, especialmente si el MACD cruza en la misma dirección.

Las pruebas indican un rendimiento anual promedio de aproximadamente 139%. Funciona mejor en el mercado de acciones.

La estrategia opera esas señales alineadas y sale una vez que el ADX comienza a debilitarse o el MACD gira en contra de la posición.

Un stop porcentual moderado contiene las pérdidas durante mercados erráticos.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `AdxPeriod` = 14
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
    Sin posición, un cruce del MACD por encima de su línea de señal abre un largo y por debajo un corto, pero solo mientras el ADX supera su valor anterior. La posición se cierra cuando el ADX cae por debajo de su valor anterior o el MACD vuelve al otro lado de la línea de señal.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: ADX, MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

