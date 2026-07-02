# Dois pedidos pendentes 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma porta StockSharp do consultor especialista MetaTrader **"Duas ordens pendentes 2"**. Ele mantém duas ordens pendentes simétricas em torno do preço de mercado e permite que o primeiro lado acionado gerencie a negociação com regras configuráveis ​​de stop-loss, take-profit e trailing. A conversão usa o alto nível StockSharp API e mantém as ideias centrais do algoritmo original enquanto expõe cada botão de ajuste por meio de parâmetros de estratégia.

## Lógica de negociação
1. A estratégia assina a série de velas selecionada (velas diárias por padrão). Quando uma vela termina, ela se torna o ponto de decisão para o próximo ciclo de negociação.
2. As ordens pendentes ativas são canceladas quando expiram ou antes que novas ordens sejam feitas. Isto garante que existam apenas os níveis mais frescos do mercado.
3. Se o spread atual estiver dentro do limite permitido e a contagem de posições/ordens ativas estiver abaixo do limite configurado, a estratégia coloca duas ordens pendentes simétricas:
   - **Modo Stop** (padrão) coloca um stop de compra acima do mercado e um stop de venda abaixo dele.
   - **Modo Limite** coloca um limite de compra abaixo do mercado e um limite de venda acima dele.
   - O sinalizador *Reverse Levels* troca os tipos de pedido para replicar a chave reversa EA original.
4. Os preços de entrada são compensados do preço de compra/venda atual pelo parâmetro *Recuo Pendente*. As ordens são ignoradas quando estão mais próximas do que a distância *Min Step* das posições existentes.
5. Os pedidos pendentes podem expirar após um determinado número de minutos. Quando a expiração for atingida, todos os pedidos restantes serão cancelados.

## Gestão de posição
- Depois que um pedido é atendido, a estratégia rastreia o preço médio de entrada e o volume do lado correspondente. Preenchimentos opostos reduzem ou fecham a posição existente antes de abrir uma nova.
- A estratégia sai das posições longas quando o preço atinge qualquer uma destas condições:
  - O preço atinge a distância do stop loss abaixo do preço médio de entrada.
  - O preço atinge a distância de lucro acima do preço médio de entrada.
  - Um trailing stop é ativado depois que o lucro excede o limite de ativação e, subsequentemente, o preço volta ao nível final (movido em etapas).
- As negociações curtas usam regras espelhadas com comparações de preços invertidas.
- Quando *Apenas uma posição* está ativado, o mecanismo aguarda que a exposição atual seja fechada antes de novas ordens pendentes serem colocadas.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `StopLossPoints` | Distância até o stop loss de proteção em pontos (0 desativa). |
| `TakeProfitPoints` | Distância até a meta de take-profit em pontos (0 a desativa). |
| `MaxPositions` | Número máximo de posições ativas e ordens pendentes simultaneamente. |
| `MinStepPoints` | Distância mínima entre o preço de entrada das negociações existentes e as novas ordens pendentes. |
| `TrailingActivatePoints` | Limite de lucro que ativa o trailing stop (0 desativa o trailing). |
| `TrailingStopPoints` | Distância entre o preço de mercado e o trailing stop, uma vez ativado. |
| `TrailingStepPoints` | Melhoria mínima de preço necessária para mover o trailing stop novamente. |
| `TradeMode` | Direção permitida para novas ordens pendentes: `Buy`, `Sell` ou `BuySell`. |
| `PendingType` | Tipo de ordens pendentes a serem colocadas: `Stop` ou `Limit`. |
| `PendingExpirationMinutes` | Vida útil dos pedidos pendentes em minutos (`0` os mantém até serem preenchidos ou cancelados manualmente). |
| `PendingIndentPoints` | Compensação da oferta/venda atual usada para calcular os preços das ordens pendentes. |
| `PendingMaxSpreadPoints` | Spread máximo permitido entre lance e pedido para colocar pedidos pendentes (`0` desativa o filtro). |
| `OnlyOnePosition` | Se `true`, impede a abertura de novas negociações até que a posição atual seja fechada. |
| `ReverseLevels` | Troca a colocação de ordens de compra e venda para espelhar o modo reverso EA original. |
| `CandleType` | Período usado para acionar a avaliação do sinal (diariamente por padrão). |

## Notas
- As distâncias de preço são expressas em pontos e convertidas automaticamente para o tamanho do tick do instrumento.
- A estratégia depende de métodos auxiliares StockSharp (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`) para registro de pedidos e usa `CancelActiveOrders` para zerar o livro cada vez que uma nova decisão é tomada.
- A lógica de trailing stop é avaliada em velas finalizadas. Para comportamento de rastreamento intrabarra, use um `CandleType` mais curto.
