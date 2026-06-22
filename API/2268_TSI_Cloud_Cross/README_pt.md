# Estratégia de Cruzamento de Nuvem TSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Cruzamento de Nuvem TSI compara o True Strength Index (TSI) com uma cópia atrasada de si mesmo para formar uma nuvem. Uma posição comprada é aberta quando o TSI cruza acima da linha deslocada, indicando momentum de alta. Uma posição vendida é aberta quando o TSI cruza abaixo da linha deslocada. Os sinais podem ser invertidos e posições opostas podem ser fechadas opcionalmente.

## Detalhes

- **Critérios de entrada**:
  - TSI cruza acima do seu valor deslocado (comprado).
  - TSI cruza abaixo do seu valor deslocado (vendido).
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Fechamento opcional com sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `LongLength` = 25
  - `ShortLength` = 13
  - `TriggerShift` = 1
  - `Invert` = false
- **Filtros**:
  - Categoria: Oscilador de momentum
  - Direção: Comprado/Vendido
  - Indicadores: True Strength Index
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
