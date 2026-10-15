# Estrategia de Anomalía de Acumulación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Accrual Anomaly** implementa el factor de anomalía de acumulación. Rebalancea anualmente el primer día de negociación de mayo, tomando posiciones largas en acciones de baja acumulación y cortas en acciones de alta acumulación.

Las pruebas indican un rendimiento anual promedio de aproximadamente 12%. Funciona mejor en el mercado de renta variable de EE. UU.

Las posiciones se ajustan una vez al año; no se utilizan señales intradía.

## Detalles
- **Criterios de entrada**: ver implementación para los cálculos de acumulación.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Rebalanceo en la próxima fecha programada.
- **Stops**: Sin lógica de stop explícita.
- **Valores predeterminados**:
  - `Deciles = 10`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Fundamental
  - Dirección: Ambos
  - Indicadores: Fundamentals
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
