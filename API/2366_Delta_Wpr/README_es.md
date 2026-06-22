# Delta WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Delta WPR compara un oscilador Williams %R rápido y uno lento para capturar cambios de momentum. Cuando el valor rápido supera al lento y el oscilador lento se mantiene por encima de un nivel umbral, la estrategia abre una posición larga y cierra cualquier exposición corta. La configuración opuesta — rápido por debajo del lento con el oscilador lento por debajo del nivel — desencadena una entrada corta. Cada nueva vela se procesa solo después de completarse para evitar el ruido.

Los backtests con datos de 4 horas muestran que este enfoque funciona mejor en mercados laterales donde el Williams %R oscila entre zonas de sobrecompra y sobreventa.

## Detalles

- **Criterios de entrada**:
  - Largo: `WPR slow > Level && WPR fast > WPR slow`
  - Corto: `WPR slow < Level && WPR fast < WPR slow`
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `FastPeriod` = 14
  - `SlowPeriod` = 30
  - `Level` = -50m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: WilliamsR
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: 4h
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
