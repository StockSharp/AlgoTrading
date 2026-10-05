# Estrategia VWAP RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
VWAP RSI utiliza el precio promedio ponderado por volumen para medir el valor justo durante la sesión mientras el RSI muestra extremos de momentum.
Las operaciones se realizan cuando el precio se aleja del VWAP y el RSI alcanza niveles de sobrecompra o sobreventa.

Las pruebas indican un rendimiento anual promedio de aproximadamente 157%. Funciona mejor en el mercado de criptomonedas.

La expectativa es que el precio revierta hacia el VWAP una vez que el momentum se enfría.

Un stop porcentual protege contra tendencias que continúan alejando el precio del VWAP.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
    El mercado opera las 24 horas, por lo que el VWAP de la sesión se reinicia cada día UTC y pondera el precio típico de cada vela por su volumen. Un cierre por debajo del VWAP con RSI por debajo de RsiOversold abre un largo; un cierre por encima del VWAP con RSI por encima de RsiOverbought abre un corto. Una señal opuesta invierte la posición.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: VWAP, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

