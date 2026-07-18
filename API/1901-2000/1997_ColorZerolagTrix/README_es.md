# Estrategia Color Zerolag TRIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia agrega cinco indicadores TRIX con diferentes períodos y pesos para producir una línea rápida y una línea lenta suavizada. Las operaciones se desencadenan cuando la línea rápida cruza la línea lenta.

- **Entrada larga:** la línea rápida anterior > la lenta anterior y la rápida actual < la lenta actual.
- **Entrada corta:** la línea rápida anterior < la lenta anterior y la rápida actual > la lenta actual.
- **Gestión de posición:** indicadores opcionales permiten activar o desactivar las entradas y salidas largas/cortas por separado.
- **Parámetros:** factor de suavizado y cinco pares de períodos TRIX con sus pesos correspondientes.
- **Indicadores:** TRIX (cinco instancias) con suma ponderada y suavizado.
- **Marco temporal predeterminado:** velas de 4 horas.

## Filtros
- Categoría: Seguimiento de tendencia
- Dirección: Ambos
- Indicadores: Múltiples
- Stops: No
- Complejidad: Moderado
- Marco temporal: Largo plazo
- Estacionalidad: No
- Redes neuronales: No
- Divergencia: No
- Nivel de riesgo: Medio
