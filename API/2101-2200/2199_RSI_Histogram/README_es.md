# Estrategia de Histograma RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el histograma del Índice de Fuerza Relativa (RSI) para detectar reversiones cuando el oscilador abandona zonas extremas. El histograma colorea el valor del RSI en función de dos umbrales: un nivel alto que marca la zona de sobrecompra y un nivel bajo que marca la zona de sobreventa. Cuando el color cambia de verde (sobrecompra) a gris o rojo, la estrategia cierra posiciones cortas y abre una posición larga. Cuando el color cambia de rojo (sobreventa) a gris o verde, cierra posiciones largas y abre una posición corta.

La implementación está construida con la API de alto nivel de StockSharp y se suscribe a datos de velas de un marco temporal seleccionado. Un indicador RSI procesa las velas y genera señales siempre que su valor salga de las zonas definidas. Los parámetros opcionales permiten habilitar o deshabilitar entradas y salidas para cada lado por separado.

La estrategia tiene fines educativos y demuestra cómo convertir un asesor experto MQL al framework StockSharp.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La barra anterior estaba por encima del nivel alto y la última barra cayó por debajo de él.
  - **Corto**: La barra anterior estaba por debajo del nivel bajo y la última barra subió por encima de él.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - La señal opuesta cierra la posición actual si está permitido.
- **Stops**: Sin stops integrados; el framework `StartProtection` está preparado para añadirlos.
- **Valores predeterminados**:
  - `RSI period` = 14
  - `High level` = 60
  - `Low level` = 40
  - `Timeframe` = 4 hours
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: Opcional
  - Complejidad: Simple
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
