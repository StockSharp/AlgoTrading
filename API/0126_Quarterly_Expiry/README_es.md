# Estrategia de Vencimiento Trimestral
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Las semanas de Vencimiento Trimestral ven cómo los contratos de futuros y opciones se renuevan, creando a menudo volatilidad al cerrarse o rolarse posiciones.
Las oscilaciones de precios pueden acelerarse cuando se ajustan las coberturas y la liquidez se reduce temporalmente.

Las pruebas indican un retorno anual promedio de aproximadamente el 115%. Funciona mejor en el mercado de acciones.

La estrategia opera en la dirección de la tendencia predominante al inicio de la semana, saliendo antes del día de liquidación para evitar el caos.

Un stop fijo mantiene el riesgo bajo control si la volatilidad resulta ser demasiado extrema.

## Detalles

- **Criterios de entrada**: activadores de efecto de calendario
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

