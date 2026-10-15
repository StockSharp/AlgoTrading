# Estrategia del Efecto de Enero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
El Efecto de Enero observa que las acciones de pequeña capitalización suelen superar el rendimiento a principios de año, posiblemente debido a las ventas por pérdidas fiscales en diciembre.
Los operadores intentan capturar esta tendencia comprando a finales de diciembre y vendiendo después de las primeras semanas de enero.

Las pruebas indican un retorno anual promedio de aproximadamente el 103%. Funciona mejor en el mercado de acciones.

La estrategia sigue ese calendario, entrando cerca del fin de año y saliendo a mediados de enero.

Un stop-loss garantiza que las pérdidas se mantengan manejables si el efecto no aparece.

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

