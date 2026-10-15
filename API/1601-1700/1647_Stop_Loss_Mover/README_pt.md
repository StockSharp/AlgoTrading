# Estratégia de Movimentação de Stop Loss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utilitária monitora uma posição aberta e move seu stop-loss para o preço de entrada quando o mercado atinge um nível predefinido. Ela assina dados de velas e verifica cada vela concluída. Para posições compradas, uma vez que a máxima da vela excede o `MoveSlPrice` configurado, uma ordem stop no preço de entrada é colocada. Para posições vendidas, o stop é movido quando a mínima da vela cai abaixo do nível.

A estratégia não gera novos sinais de negociação. Abre uma única posição comprada no início para fins de demonstração e depois a protege movendo o stop para o ponto de equilíbrio quando as condições são atendidas. Isso permite aos traders garantir lucros enquanto deixam a operação correr.

## Detalhes

- **Critérios de entrada**: Uma posição comprada é aberta no início. Nenhum sinal adicional é utilizado.
- **Comprado/Vendido**: Suporta ambos, mas o exemplo abre uma posição comprada.
- **Critérios de saída**: A posição é encerrada quando a ordem stop no preço de entrada é acionada.
- **Stops**: O stop-loss é movido para o preço de entrada quando `MoveSlPrice` é atingido.
- **Valores padrão**:
  - `MoveSlPrice` = 0 (deve ser ajustado antes de executar).
  - `CandleType` = período de 1 minuto.
- **Filtros**:
  - Categoria: Gestão de risco
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Simples
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
