# Estrategia de Fortaleza de Fin de Mes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Fortaleza de Fin de Mes observa que las acciones a menudo repuntan durante los últimos días de negociación a medida que los gestores de cartera ajustan sus posiciones.
La presión compradora vinculada al window dressing puede crear un sesgo alcista confiable antes del cierre mensual.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 94%. Funciona mejor en el mercado de acciones.

La estrategia compra cerca de los últimos días del mes y sale en el primer día de negociación del nuevo mes para capturar esa tendencia.

Los stops se colocan por debajo del soporte reciente para protegerse contra una debilidad inesperada.

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

