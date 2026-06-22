# Estratégia de Último Preço
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia coloca ordens limitadas na melhor oferta de compra ou venda quando o último preço negociado se afasta por um intervalo definido pelo usuário. Ela monitora atualizações do livro de ordens Level1 e negociações para decidir as entradas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Último preço ≥ melhor ask + intervalo.
  - **Vendido**: Último preço ≤ melhor bid - intervalo.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Sinal oposto ou fora das sessões de negociação permitidas.
- **Stops**: Apenas stop loss.
- **Valores padrão**:
  - `Interval` = 400
  - `Min Volume` = 1
  - `Max Volume` = 900000
  - `Spread` = 200
  - `Volume` = 1
  - `Stop Loss` = 400
- **Sessões de negociação**:
  - 10:05:40 – 13:54:30
  - 14:08:30 – 15:44:30
  - 16:05:30 – 18:39:30
  - 19:15:10 – 23:44:30
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
