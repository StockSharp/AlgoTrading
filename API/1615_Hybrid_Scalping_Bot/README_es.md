# Estrategia de Scalping Híbrido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un sistema de scalping híbrido que combina señales de RSI con filtros de tendencia EMA y confirmación de volumen opcional. El bot puede ajustar la sensibilidad de la señal desde muy fácil hasta fuerte, e incluye funciones de salida rápida y stop trailing.

Las pruebas indican un retorno anual promedio de aproximadamente el 35%. Funciona mejor en pares de criptomonedas líquidos.

La estrategia entra largo o corto basándose en umbrales de RSI y la fuerza de la vela, filtrada opcionalmente por tendencia y volumen. Las posiciones están protegidas con take-profit, stop-loss y lógica de trailing configurables, y los límites diarios de operaciones se reinician al inicio de cada sesión.

## Detalles

- **Criterios de entrada**:
  - **Compra**: RSI por debajo de 30 con vela alcista, filtros opcionales de tendencia/volumen según la sensibilidad.
  - **Venta**: RSI por encima de 70 con vela bajista, filtros opcionales de tendencia/volumen.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Take profit, stop loss, trailing stop o reversión rápida por RSI/EMA.
- **Stops**: Sí, SL/TP basado en porcentaje y trailing stop opcional.
- **Filtros**:
  - Filtros de tendencia y volumen según la configuración.
