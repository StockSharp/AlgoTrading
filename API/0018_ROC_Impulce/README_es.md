# ROC Impulce
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en el impulso del Rate of Change (ROC)

Las pruebas indican un retorno anual promedio de aproximadamente 91%. Funciona mejor en el mercado de acciones.

ROC Impulse captura explosiones repentinas en el indicador Rate of Change. Los picos positivos marcados llevan a operaciones largas y los picos negativos marcados a operaciones cortas. Cuando el momentum se desvanece hacia cero se cierra la posición.

Los niveles de activación se pueden ajustar para reaccionar solo ante eventos de momentum excepcionales. Los stops basados en ATR ayudan a prevenir grandes pérdidas si el pico revierte rápidamente.


## Detalles

- **Criterios de entrada**: Señales basadas en ATR, ROC, Momentum.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RocPeriod` = 12
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR, ROC, Momentum
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Neural Networks: No
  - Divergencia: No
  - Nivel de riesgo: Medio

