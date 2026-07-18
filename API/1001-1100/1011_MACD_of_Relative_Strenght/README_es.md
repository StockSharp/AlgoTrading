# Estrategia MACD de Fuerza Relativa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula la fuerza relativa dividiendo el precio de cierre entre el máximo más alto durante un período específico y aplica el indicador MACD a ese ratio. Se abre una posición larga cuando el histograma MACD es positivo y se cierra cuando se vuelve negativo. Un stop-loss porcentual protege la operación.

## Detalles
- **Entrada**: Histograma > 0.
- **Salida**: Histograma < 0 o stop-loss.
- **Tipo**: Solo largos.
- **Indicadores**: Highest, MACD.
