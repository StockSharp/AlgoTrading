# Estratégia Zig Zag Aroon
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia combina a detecção simples de pivôs Zig Zag com o indicador Aroon. Compra quando Aroon Up cruza acima de Aroon Down e o último pivô é um máximo. Posições vendidas são abertas quando Aroon Down cruza acima de Aroon Up e o último pivô é um mínimo.

## Detalhes

- **Critérios de entrada**: Cruzamento de Aroon com pivô Zig Zag correspondente.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `ZigZagDepth` = 5
  - `AroonLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Aroon, ZigZag
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
