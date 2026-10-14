# Estrategia del Primer Día del Mes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Muchos mercados exhiben un sesgo alcista en el primer día de negociación del mes a medida que nuevo capital fluye hacia los fondos.
Los traders intentan adelantarse a este efecto comprando al cierre del mes anterior o al inicio de la sesión.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 97%. Funciona mejor en el mercado de criptomonedas.

La estrategia entra en largo al inicio del mes y sale antes de que comience el segundo día, capturando el típico aumento de la presión compradora.

Un pequeño stop protege contra sorpresas bajistas si la fortaleza esperada no se materializa.

## Detalles

- **Criterios de entrada**: desencadenadores de efecto calendario
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
    Los días son días UTC, ya que el mercado opera las 24 horas; las entradas ocurren al cierre de la primera vela del día. La compra se abre en la primera vela del mes y se cierra en la última vela de ese primer día.
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

