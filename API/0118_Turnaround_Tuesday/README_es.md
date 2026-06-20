# Estrategia Turnaround Tuesday
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Turnaround Tuesday se refiere a la tendencia de los mercados que cayeron el lunes a rebotar al día siguiente.
El efecto se atribuye a menudo a los traders que reaccionan de forma exagerada tras el fin de semana y luego invierten el rumbo.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 91%. Funciona mejor en el mercado de acciones.

Esta estrategia compra en la apertura del martes cuando el lunes fue bajista, manteniéndose solo durante la sesión o hasta que se alcanza un objetivo de beneficio modesto.

Los stops son ajustados para proteger contra una debilidad continuada si el rebote no llega a desarrollarse.

## Detalles

- **Criterios de entrada**: desencadenadores de efecto calendario
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
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

