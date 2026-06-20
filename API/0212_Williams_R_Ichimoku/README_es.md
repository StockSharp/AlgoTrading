# Williams R Ichimoku Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta configuración combina los extremos de momentum de Williams %R con la estructura de tendencia definida por la Nube Ichimoku. La idea es unirse a movimientos fuertes solo cuando el precio se encuentra en el lado favorable de la nube y las líneas de corto plazo confirman el sesgo.

Las pruebas indican un rendimiento anual promedio de aproximadamente 73%. Funciona mejor en el mercado de criptomonedas.

Una oportunidad larga aparece cuando el oscilador cae por debajo de -80 mientras el precio se mantiene por encima de la nube y Tenkan-sen cruza por encima de Kijun-sen. Una señal corta ocurre cuando %R sube por encima de -20 con el precio bajo la nube y Tenkan-sen por debajo de Kijun-sen. La posición permanece abierta hasta que el precio cruza el lado opuesto de la nube.

Debido a que el método espera varias confirmaciones, es adecuado para traders que prefieren filtros de tendencia claros sobre reversiones rápidas. Los stops dinámicos se establecen alrededor del Kijun-sen para que el riesgo se ajuste con la fuerza de la tendencia subyacente.

## Detalles
- **Criterios de entrada**:
  - **Largo**: %R < -80 && price above Ichimoku cloud and Tenkan-sen > Kijun-sen
  - **Corto**: %R > -20 && price below Ichimoku cloud and Tenkan-sen < Kijun-sen
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando el precio cruce por debajo de la nube
  - **Corto**: Salir cuando el precio cruce por encima de la nube
- **Stops**: Sí.
- **Valores predeterminados**:
  - `WilliamsRPeriod` = 14
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: Williams R Ichimoku
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

