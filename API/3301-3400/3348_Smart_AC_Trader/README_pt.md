# Estratégia Smart AC Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O Smart AC Trader adapta a ideia original do MetaTrader "Smart AC Trader" ao alto nível de StockSharp API. O especialista MQL avaliou a força relativa das moedas dentro de um par e reagiu quando a moeda base superou a moeda de cotação. Em StockSharp nos concentramos no mesmo comportamento impulsionado pelo momento, mas operamos em um único instrumento ao qual a estratégia está vinculada. A força é aproximada por meio de uma combinação de médias móveis exponenciais (EMAs) e do indicador de taxa de mudança (ROC):

- Um EMA rápido mede a direção da tendência de curto prazo.
- Um EMA lento representa a tendência primária.
- ROC confirma que a dinâmica do preço está alinhada com a tendência antes que as entradas sejam permitidas.

Depois que uma posição é aberta, a estratégia gerencia ativamente a negociação usando regras de stop-loss, take-profit, trailing stop e ponto de equilíbrio que refletem a extensa configuração de gerenciamento de dinheiro do especialista original.

## Lógica de negociação
1. Assine o tipo de vela configurado (período de tempo) e calcule o EMA rápida, EMA lenta e ROC no fechamento da vela.
2. Insira uma posição longa quando o EMA rápido estiver acima do EMA lento e ROC for maior ou igual ao limite do impulso de compra. A exposição curta existente é fechada antes que a nova posição longa seja aberta.
3. Insira uma posição curta quando o EMA rápido estiver abaixo do EMA lento e ROC for menor ou igual ao limite negativo do impulso de venda. A exposição longa existente é fechada antes que a nova posição curta seja aberta.
4. Gerencie uma posição aberta em cada vela finalizada:
   - Feche a negociação nas distâncias configuradas de take-profit ou stop-loss (expressas em etapas de preço).
   - Opcionalmente, armar uma saída de equilíbrio quando o preço se mover a favor da negociação pela distância de gatilho e liquidar se o preço retornar à compensação preservada.
   - Opcionalmente, siga a parada pela distância configurada da máxima mais alta (longa) ou da mínima mais baixa (curta) observada após a entrada.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| **EMA rápida** | Comprimento do filtro de tendência rápida EMA. |
| **EMA lenta** | Comprimento do filtro de tendência lenta EMA. |
| **ROC Período** | Janela de lookback para o filtro de momentum da taxa de mudança. |
| **Compre impulso** | Mínimo positivo ROC necessário para abrir negociações longas. |
| **Vender impulso** | Negativo absoluto mínimo ROC necessário para abrir negociações curtas. |
| **Stop Loss** | Distância de stop-loss expressa em etapas de preço. |
| **Receba lucro** | Distância de lucro expressa em etapas de preço. |
| **Usar rastreamento** | Permite o gerenciamento de trailing stop. |
| **Trilhando** | Distância do trailing stop em etapas de preço. |
| **Use o ponto de equilíbrio** | Habilita a lógica de proteção de ponto de equilíbrio. |
| **Acionador do ponto de equilíbrio** | Lucro nas etapas de preços necessárias para armar a lógica do ponto de equilíbrio. |
| **Compensação do ponto de equilíbrio** | Distância nas etapas de preço mantidas após o gatilho do ponto de equilíbrio ser atingido. |
| **Tipo de vela** | Tipo de vela utilizado para alimentar os indicadores. |

## Notas
- A estratégia usa `Strategy.StartProtection()` uma vez na inicialização para garantir que o sistema integrado de proteção de posição esteja ativo conforme recomendado pelas diretrizes do projeto.
- O dimensionamento da posição depende da propriedade base `Strategy.Volume`. As ordens de reversão incluem automaticamente a exposição atual para que um sinal oposto feche a posição existente e estabeleça uma nova.
- Todos os parâmetros de risco são expressos em etapas de preço porque o consultor especialista original usou distâncias baseadas em pip. Certifique-se de que o instrumento tenha um `PriceStep` válido configurado.
