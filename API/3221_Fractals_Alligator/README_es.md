# Estrategia de Fractals & Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el experto "Fractals & Alligator" de MetaTrader combinando la alineación del Alligator de Bill Williams con rupturas de fractales, una capa de confirmación de momentum y filtros de rango. Procesa velas terminadas en un marco temporal superior para emular la lógica multi-temporal original.

## Detalles
- **Criterios de entrada**: Esperar a que los labios, dientes y mandíbula del Alligator se ensanchen en la misma dirección mientras se forma un fractal fresco más allá de la boca. Una configuración larga requiere que el cierre rompa el último fractal alcista por encima de los dientes y que cualquiera de las últimas tres lecturas de momentum supere el umbral de compra. Los cortos reflejan las reglas en la parte inferior.
- **Largo/Corto**: Abre operaciones tanto largas como cortas. Solo se mantiene una posición neta; las nuevas señales revierten la exposición existente.
- **Criterios de salida**: Las posiciones se cierran cuando se penetra el fractal opuesto o cuando la alineación del Alligator colapsa. Las órdenes protectoras manejan las salidas restantes.
- **Stops**: Usa órdenes protectoras de StockSharp para stop-loss, take-profit y un trailing stop opcional en pasos de precio, coincidiendo con la idea de gestión monetaria original.
- **Valores predeterminados**: Longitudes del Alligator 13/8/5 con desplazamientos 8/5/3, momentum de 14 períodos, retroceso de rango de 10 barras, caja fija de 20 pasos (si el filtro ATR está desactivado), take-profit 50 pasos, stop-loss 20 pasos, trailing stop 40 pasos.
- **Filtros**: El multiplicador ATR opcional confirma que el precio se ha movido al menos un ATR desde el rango reciente; de lo contrario, se usa una caja fija expresada en pasos de precio. Los umbrales de momentum (0.3%) suprimen las rupturas de baja energía.
