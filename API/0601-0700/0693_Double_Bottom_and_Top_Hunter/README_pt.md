# Estratégia Caçadora de Duplo Fundo e Duplo Topo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia busca padrões de duplo fundo e duplo topo comparando mínimas e máximas recentes. Um duplo fundo ocorre quando a mínima mais baixa é atingida duas vezes dentro de uma janela de retrospectiva mais ampla, enquanto o duplo topo usa a máxima mais alta. Posições compradas e vendidas são abertas de acordo e fechadas quando o preço rompe o extremo oposto após a formação de um novo extremo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Duplo fundo detectado.
  - **Vendido**: Duplo topo detectado.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Comprado: Nova máxima acima da máxima anterior com o preço caindo abaixo da mínima anterior.
  - Vendido: Nova mínima abaixo da mínima anterior com o preço subindo acima da máxima anterior.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 100
  - `Lookback` = 100
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
