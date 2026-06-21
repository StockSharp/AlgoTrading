# Estrategia Ichimoku Clouds Largo y Corto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el cruce de Tenkan-sen y Kijun-sen del indicador Ichimoku. Los cruces se clasifican como fuertes, neutros o débiles según el valor de Tenkan relativo a la nube. Dependiendo del modo de trading seleccionado, abre posiciones largas o cortas cuando se produce la fuerza de señal elegida. Se pueden configurar toma de ganancias y stop-loss basados en porcentaje para cerrar posiciones, o señales opuestas.

## Detalles

- **Criterios de entrada**:
  - Tenkan-sen cruza por encima de Kijun-sen y la fuerza de señal coincide con las opciones largas seleccionadas.
  - Tenkan-sen cruza por debajo de Kijun-sen y la fuerza de señal coincide con las opciones cortas seleccionadas.
- **Largo/Corto**: Configurable, por defecto largo.
- **Criterios de salida**:
  - Señales opuestas según las opciones de salida definidas.
  - Porcentajes opcionales de toma de ganancias o stop-loss.
- **Stops**: Toma de ganancias y stop-loss porcentuales.
- **Valores predeterminados**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `TakeProfitPct` = 0
  - `StopLossPct` = 0
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Ichimoku
  - Stops: Opcional
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
