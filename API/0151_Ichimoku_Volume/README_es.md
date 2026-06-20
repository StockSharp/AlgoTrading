# Ichimoku Volume Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Implementación de estrategia - Ichimoku + Volume. Compra cuando el precio está por encima de la nube Kumo, Tenkan-sen está por encima de Kijun-sen y el volumen está por encima del promedio. Vende cuando el precio está por debajo de la nube Kumo, Tenkan-sen está por debajo de Kijun-sen y el volumen está por encima del promedio.

Las pruebas indican un retorno anual promedio de aproximadamente 40%. Funciona mejor en el mercado de criptomonedas.

Los componentes de Ichimoku definen el sesgo direccional mientras que el aumento de volumen confirma el interés. Las operaciones se abren cuando el precio se alinea con la nube y el volumen aumenta.

Es adecuado para traders que siguen rupturas de nubes con participación. El riesgo está limitado por un stop basado en ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `Price > Cloud && Tenkan > Kijun && Volume > AvgVolume`
  - Corto: `Price < Cloud && Tenkan < Kijun && Volume > AvgVolume`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Ruptura de la nube en dirección opuesta
- **Stops**: Basado en porcentaje usando `StopLoss`
- **Valores predeterminados**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `VolumeAvgPeriod` = 20
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ichimoku Cloud, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

