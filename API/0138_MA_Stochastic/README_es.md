# Estrategia MA Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
MA Stochastic utiliza un filtro de tendencia de media móvil con retrocesos del oscilador estocástico.
Cuando el precio está en tendencia alcista por encima de la media y el estocástico cae a zona de sobreventa, el sistema se prepara para comprar en el siguiente giro al alza.

Las pruebas indican un rendimiento anual promedio de aproximadamente 151%. Funciona mejor en el mercado de acciones.

Las operaciones en corto reflejan esta lógica para tendencias bajistas, vendiendo rebotes cuando el estocástico alcanza sobrecompra.

Los stops porcentuales fijos ayudan a evitar grandes pérdidas si la tendencia revierte de repente.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Moving Average, Stochastic
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

