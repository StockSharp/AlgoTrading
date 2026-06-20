# Estrategia de Reversión Spring
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Reversión Spring es un concepto de Wyckoff donde el precio rompe brevemente el soporte y luego regresa por encima de él.
Esta sacudida atrapa a los vendedores tardíos y a menudo marca el comienzo de una tendencia alcista.

Las pruebas indican un rendimiento anual promedio de aproximadamente 55%. Funciona mejor en el mercado de acciones.

La estrategia compra una vez que el precio recupera el nivel roto, anticipando un rápido cierre de cortos y nueva demanda.

Un stop justo por debajo del mínimo del spring limita la pérdida, y la posición se cierra si el seguimiento falla.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basados en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Wyckoff
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

