# Estrategia de Reversión del Canal Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Los Canales Donchian marcan los máximos y mínimos recientes durante un período elegido. Los precios que perforan esos límites y luego se revierten pueden señalar agotamiento. Esta estrategia observa cierres de vuelta dentro del canal después de una ruptura breve.

Las pruebas indican una rentabilidad anual media de aproximadamente el 157%. Funciona mejor en el mercado de criptomonedas.

Si el cierre anterior estaba por debajo de la banda inferior y el cierre actual vuelve a subir por encima de ella, se toma una operación larga. A la inversa, si el cierre anterior estaba por encima de la banda superior y el precio cae de vuelta dentro, se abre una posición corta. Un stop porcentual gestiona el riesgo en ambos casos.

Al operar solo después de una ruptura fallida, este enfoque intenta capturar movimientos falsos que se retraen rápidamente.

## Detalles

- **Criterios de entrada**: El precio cierra de vuelta dentro del Canal Donchian tras superar la banda superior o inferior.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss.
- **Stops**: Sí, basado en porcentaje.
- **Valores predeterminados**:
  - `Period` = 20
  - `StopLoss` = 2%
  - `CandleType` = 15 minute
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Donchian Channel
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

