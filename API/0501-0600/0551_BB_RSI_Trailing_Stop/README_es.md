# Estrategia BB RSI con Stop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina Bollinger Bands con el momentum del RSI y protege las operaciones con un stop trailing condicional.
Las posiciones largas ocurren cuando el precio perfora la banda inferior y el RSI sale de la zona de sobreventa. Los cortos se activan en la banda superior con RSI en sobrecompra.

El stop-loss comienza a una distancia fija y se convierte en stop trailing una vez que el precio se mueve favorablemente un desplazamiento preestablecido.

## Detalles

- **Criterios de entrada**: Ruptura de Bollinger Band con confirmación RSI
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop-loss inicial o stop trailing
- **Stops**: Sí, trailing dinámico
- **Valores predeterminados**:
  - `BollingerPeriod` = 25
  - `BollingerDeviation` = 2
  - `RsiPeriod` = 14
  - `RsiOverbought` = 60
  - `RsiOversold` = 33
  - `StopLossPoints` = 50
  - `TrailOffsetPoints` = 99
  - `TrailStopPoints` = 40
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, RSI
  - Stops: Trailing
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
