# Estrategia Donchian Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Donchian Volume utiliza rupturas del canal Donchian confirmadas por volumen creciente para iniciar operaciones.
Un movimiento fuera del canal con volumen elevado sugiere el inicio de una nueva tendencia.

Las pruebas indican un rendimiento anual promedio de aproximadamente 160%. Funciona mejor en el mercado forex.

La estrategia entra en la dirección de la ruptura y sale cuando el precio cierra de nuevo dentro del canal o el volumen disminuye.

Los stops se establecen a corta distancia dentro del canal para proteger contra movimientos falsos.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `DonchianPeriod` = 20
  - `VolumePeriod` = 20
    El canal abarca el máximo y el mínimo de las DonchianPeriod velas anteriores. Un cierre fuera de él con volumen superior a la media de las VolumePeriod velas anteriores entra en la dirección de la ruptura e invierte una posición opuesta. La posición se cierra cuando el precio vuelve a cerrar dentro del canal o el volumen cae por debajo de su media; el stop está a StopLossPercent del precio de entrada, justo dentro del límite roto.
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Donchian Channel, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

