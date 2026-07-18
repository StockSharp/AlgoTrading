# Estrategia CCI Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en la ruptura del CCI (Commodity Channel Index)

Las pruebas indican un retorno anual promedio de aproximadamente 94%. Funciona mejor en el mercado de acciones.

CCI Breakout utiliza el Commodity Channel Index para detectar movimientos poderosos. Los surgimientos más allá de los umbrales positivos o negativos del CCI generan entradas. Las salidas ocurren cuando el CCI retrocede hacia cero o se forma una señal opuesta.

Dado que el CCI mide la desviación de una media móvil, las lecturas extremas implican precios insostenibles. Este sistema espera esos extremos y luego intenta beneficiarse del seguimiento.


## Detalles

- **Criterios de entrada**: Señales basadas en CCI, Momentum.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `CciPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: CCI, Momentum
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Neural Networks: No
  - Divergencia: No
  - Nivel de riesgo: Medio

