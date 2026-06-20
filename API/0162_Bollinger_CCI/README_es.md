# Bollinger Cci Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia - Bollinger Bands + CCI. Compra cuando el precio está por debajo de la banda inferior de Bollinger y el CCI está por debajo de -100 (sobreventa). Vende cuando el precio está por encima de la banda superior de Bollinger y el CCI está por encima de 100 (sobrecompra).

Las pruebas indican un rendimiento anual promedio de aproximadamente 73%. Funciona mejor en el mercado de criptomonedas.

Las bandas de Bollinger mapean los límites de volatilidad, y el CCI mide la distancia desde la media. Las rupturas más allá de una banda con confirmación del CCI desencadenan operaciones.

Adecuado para mercados volátiles donde las tendencias se extienden rápidamente. Se aplican stops basados en ATR para mayor seguridad.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close < LowerBand && CCI < CciOversold`
  - Corto: `Close > UpperBand && CCI > CciOverbought`
- **Largo/Corto**: Ambos
- **Criterios de salida**: El precio regresa a la banda media
- **Stops**: Basados en ATR usando `StopLoss`
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, CCI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

