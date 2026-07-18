# Estratégia de Grade Aeron Robot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um sistema de hedge baseado em grade inspirado no assessor especialista AeronRobot. Ela coloca ordens de compra e venda em intervalos de preço predefinidos e aumenta o volume da posição após cada nova ordem. A abordagem busca capturar pequenas oscilações de preço enquanto controla o risco por meio de take-profit, stop-loss e limites de operações configuráveis.

A estratégia funciona tanto com posições compradas quanto vendidas. Quando o preço se move em passos definidos pelo parâmetro *Gap*, uma nova ordem é aberta com volume multiplicado por *LotsFactor*. Os lucros são assegurados quando o preço retorna *TakeProfit* pontos, e as perdas são cortadas se o movimento atingir *StopLoss* pontos. A flag *Hedging* permite manter posições em ambos os lados simultaneamente.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o preço cai `Gap` pontos do último preço de compra.
  - **Vendido**: o preço sobe `Gap` pontos do último preço de venda.
- **Gerenciamento de volume**: o volume de cada nova ordem é multiplicado por `LotsFactor`.
- **Critérios de saída**:
  - as posições de um lado são encerradas quando o lucro supera os pontos `TakeProfit`.
  - as posições de um lado são encerradas quando a perda supera os pontos `StopLoss`.
- **Parâmetros**:
  - `FirstLot` – volume inicial da ordem.
  - `LotsFactor` – multiplicador para ordens subsequentes.
  - `Gap` – distância base entre níveis de grade em pontos.
  - `GapFactor` – multiplicador que expande o intervalo após cada operação.
  - `MaxTrades` – número máximo de operações por lado.
  - `Hedging` – permitir posições compradas e vendidas simultâneas.
  - `TakeProfit` – alvo em pontos.
  - `StopLoss` – limite protetor em pontos.
  - `CandleType` – período de velas usado para processamento.
- **Comprado/Vendido**: ambos.
- **Filtros**:
  - Categoria: Grade / Reversão à média
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto

