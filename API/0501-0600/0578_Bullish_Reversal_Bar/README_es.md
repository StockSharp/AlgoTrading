# Estrategia de Barra de Reversión Alcista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia - Barra de Reversión Alcista. Entra en largo cuando se forma una barra de reversión alcista por debajo de las líneas del Alligator y el precio rompe por encima del máximo de la barra. Los filtros opcionales pueden habilitar el Awesome Oscillator y las barras squat del Market Facilitation Index.

La configuración busca un nuevo mínimo que cierre en la mitad superior de la vela mientras la tendencia se vuelve alcista. La confirmación llega cuando el precio supera el máximo de la barra.

## Detalles

- **Criterios de entrada**:
  - Largo: `bullish reversal bar && close > confirmation level`
- **Largo/Corto**: Solo largos
- **Criterios de salida**:
  - Stop-loss en el mínimo de la barra o cuando la tendencia gira a la baja
- **Stops**: Mínimo de la barra almacenado en `_stopLoss`
- **Valores predeterminados**:
  - `EnableAo` = false
  - `EnableMfi` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo
  - Indicadores: Alligator, Awesome Oscillator, Market Facilitation Index
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
