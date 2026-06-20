# Estrategia Williams VIX Fix
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Williams VIX Fix adapta el indicador de volatilidad de Larry Williams a
instrumentos que carecen de un VIX publicado. Calcula un valor VIX sintético utilizando
la distancia entre el cierre más alto durante un período de referencia y el mínimo actual.
Cuando este valor sube por encima de un umbral de Bollinger Band o el precio cierra por
debajo de la banda inferior de Bollinger, la estrategia lo considera una oportunidad de
sobreventa. Un cálculo invertido mide los extremos de sobrecompra.

El enfoque busca reversión a la media después de picos de volatilidad. Cuando el VIX Fix
señala alto miedo y el precio está por debajo de la banda inferior, se abre una operación
larga. Por el contrario, cuando el VIX Fix inverso apunta a complacencia extrema y el
precio está por encima de la banda superior, las posiciones largas existentes se cierran.
Los umbrales de percentil controlan la sensibilidad.

## Detalles

- **Criterios de entrada**:
  - VIX Fix ≥ banda superior o percentil y precio < banda inferior de Bollinger.
- **Largo/Corto**: Entradas largas con salidas en señal opuesta.
- **Criterios de salida**:
  - VIX Fix invertido ≥ banda superior o percentil y precio > banda superior de Bollinger.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `WvfPeriod` = 20
  - `WvfLookback` = 50
  - `HighestPercentile` = 0.85
  - `LowestPercentile` = 0.99
- **Filtros**:
  - Categoría: Reversión a la media de volatilidad
  - Dirección: Largo
  - Indicadores: Bollinger Bands, Williams VIX Fix
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
