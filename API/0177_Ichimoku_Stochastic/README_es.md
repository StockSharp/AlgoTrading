# Ichimoku Stochastic Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en los indicadores Ichimoku Cloud y Stochastic Oscillator.
Entra largo cuando el precio está por encima de Kumo (nube), Tenkan > Kijun, y el Stochastic está en sobrevendido (< 20). Entra corto cuando el precio está por debajo de Kumo, Tenkan < Kijun, y el Stochastic está en sobrecomprado (> 80).

Las pruebas indican un retorno anual promedio de aproximadamente 118%. Funciona mejor en el mercado de acciones.

Ichimoku define la tendencia y los niveles de soporte mientras Stochastic determina el momento de entrada en los retrocesos. Las operaciones se abren cuando el oscilador se resetea dentro de la dirección predominante de la nube.

Los traders que prefieren indicadores estructurados pueden encontrarlo práctico. Los stops de ATR cubren reversiones abruptas.

## Detalles

- **Criterios de entrada**:
  - Largo: `Price > Cloud && StochK < 20`
  - Corto: `Price < Cloud && StochK > 80`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Ruptura de la nube en dirección contraria
- **Stops**: Usa los límites de la nube Ichimoku
- **Valores predeterminados**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouPeriod` = 52
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(30).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Ichimoku Cloud, Stochastic Oscillator
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

