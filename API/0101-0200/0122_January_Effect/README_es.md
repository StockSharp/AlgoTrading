# Estrategia del Efecto de Enero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este ejemplo opera **ventanas recurrentes cada mes** con un filtro de media móvil. **No** se limita al efecto clásico de las empresas de pequeña capitalización entre finales de diciembre y principios de enero ni usa datos de tamaño empresarial o ventas fiscales. El retorno anual del 103% antes indicado no está probado por BTC/TON.

### Reglas mensuales ejecutadas (C# y Python)

La implementación es una adaptación **mensual**, no exclusiva de diciembre/enero. En el primer cierre elegible de 5 minutos de un nuevo día 1–5, compra si está sin posición y `Close > SMA20`. En días 14–17 cierra largos y puede abrir cortos si está plano y `Close < SMA20`. En días 26–31 cubre cortos. Las acciones requieren un nuevo día y respetan 50 velas terminadas de espera. Un stop nativo del 2% protege largos y cortos ejecutados independientemente de estas fechas. La rentabilidad indicada arriba no está confirmada con BTC/TON.


## Detalles

- **Criterios de entrada**: activadores de efecto de calendario
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 5 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Ambos
  - Indicadores: Estacionalidad
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

