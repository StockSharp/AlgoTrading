# Estratégia DeMarker Sign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o oscilador DeMarker para detectar possíveis reversões de tendência. Em cada vela completada (período de 4 horas por padrão), o valor do DeMarker é comparado com limites superior e inferior configuráveis. Quando o oscilador sobe acima do limite inferior (0.3 por padrão), a estratégia entra em uma posição comprada e fecha qualquer posição vendida. Quando o oscilador cai abaixo do limite superior (0.7 por padrão), entra em uma posição vendida e fecha qualquer posição comprada. As posições são mantidas até que um sinal oposto apareça.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: DeMarker cruza para cima pelo nível inferior.
  - **Vendido**: DeMarker cruza para baixo pelo nível superior.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum por padrão.
- **Filtros**: Nenhum.
