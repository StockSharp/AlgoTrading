# Estrategia Donchian Macd
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia que combina la ruptura del Canal Donchian con la confirmación de tendencia mediante MACD.

Las pruebas indican un retorno anual promedio de aproximadamente 148%. Funciona mejor en el mercado forex.

La estrategia espera una ruptura de Donchian y verifica el momentum con MACD. Las operaciones largas o cortas siguen el movimiento una vez que MACD está de acuerdo.

Dirigida a entusiastas de las rupturas que desean confirmación. Los stops se colocan usando un multiplicador de ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `Price breaks Donchian high && MACD > Signal`
  - Corto: `Price breaks Donchian low && MACD < Signal`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Reversión del MACD
- **Stops**: Porcentual usando `StopLossPercent`
- **Valores predeterminados**:
  - `DonchianPeriod` = 20
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Donchian Channel, MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

