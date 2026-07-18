# Estrategia Color XXDPO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza un Oscilador de Precio Depurado doblemente suavizado para capturar reversiones de pendiente.

## Detalles
- **Criterios de entrada**: La pendiente ascendente con el valor actual en alza abre largo; la pendiente descendente con el valor actual en baja abre corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: El cambio de pendiente opuesto cierra las posiciones.
- **Stops**: Ninguno.
- **Valores predeterminados**: Longitud de la primera MA 21, longitud de la segunda MA 5, marco temporal de velas 6 horas.
- **Filtros**: Ninguno.
