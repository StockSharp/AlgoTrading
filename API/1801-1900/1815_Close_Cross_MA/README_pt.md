# Estratégia de Fechamento por Cruzamento de MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia monitora uma média móvel simples (MA) e fecha automaticamente qualquer posição aberta quando o fechamento do candle cruza a linha da MA. É projetada para traders que gerenciam entradas manualmente ou com outros sistemas, mas desejam uma saída automatizada quando a tendência se inverte.

A lógica rastreia a relação entre o preço de fechamento e a MA. Quando um novo candle concluído cruza de um lado da MA para o outro, a estratégia envia uma ordem a mercado para fechar a posição. Nenhuma nova posição é aberta.

## Detalhes

- **Critérios de entrada**: Nenhum. As posições devem ser abertas externamente.
- **Critérios de saída**:
  - **Comprado**: Fechamento anterior acima da MA e fechamento atual abaixo da MA aciona uma venda para fechar.
  - **Vendido**: Fechamento anterior abaixo da MA e fechamento atual acima da MA aciona uma compra para fechar.
- **Comprado/Vendido**: Ambas as direções são suportadas.
- **Stops**: Não utilizados. O cruzamento da MA atua como sinal de saída.
- **Valores padrão**:
  - `MA Period` = 50.
  - `Candle Type` = Período de 1 minuto.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Não
  - Complexidade: Simples
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado

