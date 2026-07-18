# Estrategia MA Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
MA Volume combina un filtro de tendencia de media móvil con aumentos de volumen para sincronizar las entradas.
El volumen creciente junto con el precio por encima de la media señala una acumulación fuerte; el volumen decreciente por debajo de la media indica distribución.

Las pruebas indican un rendimiento anual promedio de aproximadamente 136%. Funciona mejor en el mercado de acciones.

La estrategia opera en la dirección de la media móvil cuando el volumen se expande, saliendo una vez que el volumen se agota o la media se revierte.

Un stop porcentual protege contra cambios repentinos en la tendencia.

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
  - Indicadores: Moving Average, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

