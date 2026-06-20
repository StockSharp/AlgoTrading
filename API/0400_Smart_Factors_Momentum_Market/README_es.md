# Estrategia de Factores Inteligentes y Momentum de Mercado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Factores Inteligentes y Momentum de Mercado** combina múltiples factores de renta variable con un filtro de tendencia del mercado amplio. El sistema toma posiciones largas en el mercado solo cuando tanto la cesta de factores de momentum como el índice general muestran tendencias positivas; de lo contrario, permanece en efectivo.

## Detalles
- **Criterios de entrada**: Confirmación de momentum compuesto de factores y tendencia de mercado.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Salir cuando el momentum de factores o la tendencia de mercado se vuelve negativa.
- **Stops**: Sin stop explícito.
- **Valores predeterminados**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo
  - Indicadores: Múltiples
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
