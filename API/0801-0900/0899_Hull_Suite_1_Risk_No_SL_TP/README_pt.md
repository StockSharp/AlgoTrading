# Estratégia Hull Suite – Risco 1%, Sem SL/TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Hull Suite abre posições compradas quando a média móvel Hull selecionada sobe em comparação com duas barras atrás, e abre posições vendidas quando cai. Nenhum stop loss ou take profit é utilizado.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O valor de Hull é maior que o valor de duas barras atrás.
  - **Vendido**: O valor de Hull é menor que o valor de duas barras atrás.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Reverter posição ao sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `HullLength` = 55
  - `Mode` = Hma
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: HMA, EHMA, THMA
  - Stops: Nenhum
  - Complexidade: Baixo
  - Período: 5m
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
