# Estrategia CCI COMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza el Índice de Canal de Materias Primas (CCI) y medias móviles de múltiples marcos temporales para seguir la tendencia predominante.

## Detalles

- **Datos**: Velas de precios de múltiples marcos temporales.
- **Entrada**: Largo cuando CCI está por encima de cero, RSI por encima de 50, la vela cierra por encima de la apertura y todos los marcos temporales monitorizados muestran una tendencia alcista; corto cuando ocurre lo contrario.
- **Salida**: La posición se cierra con la señal opuesta.
- **Instrumentos**: Cualquier instrumento.
- **Riesgo**: Sin stop-loss ni take-profit explícitos.
