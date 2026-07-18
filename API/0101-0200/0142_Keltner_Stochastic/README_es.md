# Estrategia Keltner Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia que combina los Canales Keltner y el Oscilador Stochastic.
Entra en posiciones cuando el precio alcanza los límites del Canal Keltner y el Stochastic confirma condiciones de sobreventa/sobrecompra.

Las pruebas indican un retorno anual promedio de aproximadamente 163%. Funciona mejor en el mercado de acciones.

Este enfoque busca capturar reversiones cerca de las bandas Keltner mientras el oscilador confirma cambios de momentum. Las señales pueden activarse en ambas direcciones cuando el precio presiona contra un sobre.

Los traders a corto plazo que buscan reversiones rápidas pueden encontrarlo útil. El riesgo está contenido por una distancia de stop basada en ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close < LowerBand && StochK < StochOversold`
  - Corto: `Close > UpperBand && StochK > StochOverbought`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: `Close > EMA`
  - Corto: `Close < EMA`
- **Stops**: `StopLossAtr` ATR desde la entrada
- **Valores predeterminados**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `KeltnerMultiplier` = 2.0m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `StopLossAtr` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Keltner Channel, Stochastic Oscillator
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

