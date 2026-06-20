# Hull Ma Stochastic Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia Hull Moving Average + Stochastic Oscillator. La estrategia entra cuando la dirección de la tendencia del HMA cambia con el Stochastic confirmando condiciones de sobreventa/sobrecompra.

Las pruebas indican un rendimiento anual promedio de aproximadamente 94%. Funciona mejor en el mercado de acciones.

El Hull MA revela rápidamente la dirección de la tendencia. El Stochastic espera una caída o un repunte dentro de esa tendencia para desencadenar la operación.

Un enfoque flexible para quienes buscan señales suaves. Los stops basados en ATR limitan la pérdida potencial.

## Detalles

- **Criterios de entrada**:
  - Largo: `HullMA turning up && StochK < 20`
  - Corto: `HullMA turning down && StochK > 80`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Cambio de dirección del Hull MA
- **Stops**: Basados en ATR usando `StopLossAtr`
- **Valores predeterminados**:
  - `HmaPeriod` = 9
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossAtr` = 2m
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Hull MA, Moving Average, Stochastic Oscillator
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

