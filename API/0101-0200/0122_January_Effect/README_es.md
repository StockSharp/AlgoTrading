# Estrategia del Efecto de Enero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
El Efecto de Enero observa que las acciones de pequeña capitalización suelen superar el rendimiento a principios de año, posiblemente debido a las ventas por pérdidas fiscales en diciembre.
Los operadores intentan capturar esta tendencia comprando a finales de diciembre y vendiendo después de las primeras semanas de enero.

Las pruebas indican un retorno anual promedio de aproximadamente el 103%. Funciona mejor en el mercado de acciones.

La estrategia sigue ese calendario, entrando cerca del fin de año y saliendo a mediados de enero.

Un stop-loss garantiza que las pérdidas se mantengan manejables si el efecto no aparece.

### Reglas mensuales ejecutadas (C# y Python)

La implementación es una adaptación **mensual**, no exclusiva de diciembre/enero. En el primer cierre elegible de 5 minutos de un nuevo día 1–5, compra si está sin posición y `Close > SMA20`. En días 14–17 cierra largos y puede abrir cortos si está plano y `Close < SMA20`. En días 26–31 cubre cortos. Las acciones requieren un nuevo día y respetan 50 velas terminadas de espera. Un stop nativo del 2% protege largos y cortos ejecutados independientemente de estas fechas. La rentabilidad indicada arriba no está confirmada con BTC/TON.


## Detalles

- **Criterios de entrada**: activadores de efecto de calendario
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2%
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Ambos
  - Indicadores: Estacionalidad
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

