# Estratégia Viva Las Vegas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Viva Las Vegas é um divertido especialista em gerenciamento de dinheiro que compra ou vende aleatoriamente o instrumento anexado e depois deixa um dos cinco sistemas de apostas decidir o tamanho da próxima aposta. A porta StockSharp mantém o comportamento original MetaTrader ao:
- Escolher uma direção comercial por meio de um lançamento pseudo-aleatório de moeda a cada nova tentativa.
- Colocação imediata de proteções simétricas de stop-loss e take-profit expressas em pips.
- Atualizar a sequência de progressão assim que a posição anterior for fechada e abrir uma nova posição imediatamente.

A estratégia, portanto, permanece constantemente exposta (uma posição aberta por vez) e mostra como vários sistemas clássicos de apostas se comportam dentro da estrutura de negociação de StockSharp.

## Módulos de gerenciamento de dinheiro
O parâmetro `MoneyManagement` seleciona um dos seguintes modelos de piquetagem, todos os quais usam `BaseVolume` como tamanho do lote âncora:

1. **Martingale** – dobre o tamanho do lote após cada negociação perdida e redefina para o volume base após uma negociação lucrativa.
2. **Pirâmide Negativa** – dobra o tamanho do lote após uma perda, mas reduz o volume pela metade após uma vitória (nunca ficando abaixo do volume base).
3. **Labouchere** – mantenha uma sequência numérica (padrão `1-2-3`), aposte a soma do primeiro e do último número, remova-os após uma vitória e anexe sua soma após uma perda.
4. **Oscar’s Grind** – aumente a aposta no lote base após cada vitória até que um lote base de lucro tenha sido acumulado e, em seguida, reinicie; as perdas apenas diminuem o resultado da corrida.
5. **31 Sistema** – percorre a série `1,1,1,2,2,4,4,8,8`, duplicando o elemento atual após a primeira vitória e reiniciando ao início após a segunda vitória consecutiva.

Todos os módulos seguem de perto a implementação original MQL, incluindo como as progressões de volume reagem aos empates (negociações com lucro zero são tratadas como perdas).

## Fluxo de trabalho de negociação
1. No início, a estratégia semeia o gerador pseudo-aleatório (baseado no tempo quando `Seed = 0`) e ativa o mecanismo de proteção de StockSharp com paradas e alvos simétricos.
2. Quando nenhuma posição está aberta e nenhuma ordem está pendente, a estratégia solicita ao módulo de staking ativo o próximo tamanho de lote, arredonda para o `VolumeStep` do instrumento e lança uma moeda para escolher entre `BuyMarket` e `SellMarket`.
3. Uma vez estabelecida a posição, o módulo de proteção gerencia a saída utilizando a distância pip configurada.
4. Quando a posição retorna ao nível estável, o delta PnL realizado é avaliado:
   - Lucro > 0 → o módulo recebe uma notificação de **vitória**.
   - Lucro ≤ 0 → o módulo recebe uma notificação de **perda**.
5. O processo entra em loop imediatamente, de modo que a conta está sempre em negociação ou aguardando um novo preenchimento.

Como existe apenas uma posição por vez, a estratégia é fácil de seguir em um gráfico e reflete perfeitamente o comportamento de bilhete único do consultor especialista original.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `StopTakePips` | `int` | `50` | Distância (em pips) aplicada a ordens de stop-loss e take-profit via `StartProtection`. |
| `BaseVolume` | `decimal` | `1` | O tamanho do lote âncora contribuiu para a progressão da gestão de dinheiro. |
| `MoneyManagement` | `MoneyManagementMode` | `Martingale` | Algoritmo de piquetagem que controla como o tamanho do próximo pedido é calculado. |
| `Seed` | `int` | `0` | Semente geradora pseudo-aleatória. Um valor zero muda para uma semente dependente do tempo, de modo que cada execução é diferente. |

## Notas de implementação
- Os volumes são normalizados para o `VolumeStep` do instrumento e verificados em relação a `MinVolume` / `MaxVolume` para evitar pedidos rejeitados.
- As distâncias stop/take são convertidas em etapas de preço usando a regra clássica MetaTrader (`Digits` igual a 3 ou 5 implica dez ticks por pip).
- O lucro realizado é medido por meio da propriedade `PnL` da estratégia, garantindo que as saídas protetoras e os fechamentos manuais influenciem a sequência de piquetagem exatamente como no código original.
- Os comentários em inglês destacam os pontos de decisão, facilitando a adaptação do modelo para fins educacionais ou experimentos de risco controlado.

## Dicas de uso
- Escolha um conector de demonstração ou ambiente de reprodução; o algoritmo é intencionalmente arriscado e destinado à experimentação.
- Ajuste `BaseVolume` para corresponder ao tamanho do contrato do instrumento antes de iniciar a estratégia.
- Combine a estratégia com gráficos StockSharp para observar como cada sistema de staking aumenta ou contrai o tamanho da posição ao longo do tempo.
