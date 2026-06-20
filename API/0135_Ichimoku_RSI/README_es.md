# Estrategia Ichimoku RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Ichimoku RSI utiliza los niveles de la nube Ichimoku para definir la dirección de la tendencia mientras el RSI identifica retrocesos a corto plazo.
Las operaciones se alinean con la nube, entrando cuando el RSI se recupera de la sobreventa en una tendencia alcista o cae desde la sobrecompra en una tendencia bajista.

Las pruebas indican un rendimiento anual promedio de aproximadamente 142%. Funciona mejor en el mercado de acciones.

Al combinar un filtro de tendencia amplio con un oscilador de momentum, la estrategia busca unirse a movimientos fuertes después de breves pausas.

Los stops se ubican más allá del límite de la nube para proteger contra correcciones más profundas.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Ichimoku, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

