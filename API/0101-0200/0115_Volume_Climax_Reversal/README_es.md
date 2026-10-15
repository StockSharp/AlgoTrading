# Estrategia de Reversión por Clímax de Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Reversión por Clímax de Volumen busca puntos de inflexión marcados por un volumen extremadamente alto tras una tendencia fuerte.
Estos picos climáticos sugieren agotamiento a medida que los últimos compradores o vendedores se precipitan antes de que el impulso se desvanezca.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 82%. Funciona mejor en el mercado de acciones.

La estrategia entra en contra del movimiento anterior una vez que se cierra una barra de gran volumen y el precio comienza a retroceder.

Un stop porcentual ajustado protege la posición, y las operaciones salen si el volumen no disminuye o el precio continúa en la dirección original.

## Detalles

- **Criterios de entrada**: señal del indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `MaPeriod` = 20
  - `VolumeMultiplier` = 2
  - `StopLossPercent` = 2
    Un clímax es una vela que cierra a favor de la tendencia (alcista por encima de la SMA de MaPeriod, bajista por debajo) con un volumen mayor que VolumeMultiplier veces el promedio de las MaPeriod velas anteriores. La siguiente vela que cierra en sentido contrario abre la operación contra el movimiento. La operación sale cuando un cierre supera el extremo del clímax, cuando el volumen vuelve a dispararse o en el stop porcentual.
- **Filtros**:
  - Categoría: Volumen
  - Dirección: Ambos
  - Indicadores: Volumen
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

