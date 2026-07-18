# Estratégia de Spread Negativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Negative Spread explora momentos raros quando o melhor preço de venda cai abaixo do melhor preço de compra, criando um spread negativo.
Quando esse desequilíbrio de preços aparece, a estratégia vende a mercado e tenta capturar o spread anormal.
Após a abertura da posição vendida, ela é fechada na próxima atualização do livro de ordens, quando o mercado retorna a um estado normal.

O sistema escuta apenas eventos do livro de ordens e não depende de velas ou indicadores.
Parâmetros opcionais de stop-loss e take-profit são fornecidos como medidas de segurança e são calculados em pips usando o tamanho do tick do instrumento.

## Detalhes
- **Critérios de entrada**: `BestAsk < BestBid` e sem posição ativa.
- **Comprado/Vendido**: Somente vendido.
- **Critérios de saída**: A posição é fechada imediatamente após ser aberta.
- **Stops**: Stop-loss e take-profit opcionais em pips.
- **Valores padrão**:
  - `Volume` = 1
  - `TakeProfitPips` = 5000
  - `StopLossPips` = 5000
- **Filtros**:
  - Categoria: Arbitragem
  - Direção: Vendido
  - Indicadores: Nenhum
  - Stops: Opcional
  - Complexidade: Básico
  - Período: Tick
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
