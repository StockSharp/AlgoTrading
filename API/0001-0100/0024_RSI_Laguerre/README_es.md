# Estrategia RSI Laguerre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el RSI Laguerre

Las pruebas indican un rendimiento anual promedio de aproximadamente 109%. Funciona mejor en el mercado de criptomonedas.

El RSI Laguerre suaviza el RSI estándar para reducir el ruido. La estrategia compra cuando el valor Laguerre cruza hacia arriba desde la zona de sobreventa y vende cuando cruza hacia abajo desde la sobrecompra, saliendo cuando regresa a niveles medios.

El filtrado Laguerre ayuda a evitar condiciones agitadas que afectan las señales del RSI regular. El método es popular para capturar oscilaciones en gráficos intradía ignorando las fluctuaciones menores.


## Detalles

- **Criterios de entrada**: Señales basadas en RSI.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Gamma` = 0.7m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

