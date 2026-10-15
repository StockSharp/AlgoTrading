# Estratégia de limite de reversão de pivô TCP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia TCP Pivot Limit é uma conversão do clássico MetaTrader 4 expert **gpfTCPivotLimit.mq4**. O especialista original calcula os níveis de pivô diários e procura falsos rompimentos em torno desses níveis usando velas horárias. Assim que o rompimento falha, a estratégia entra imediatamente em uma negociação de reversão visando os níveis de pivô opostos. Esta implementação reproduz a mesma lógica usando a StockSharp estratégia de alto nível API.

A estratégia opera em velas horárias e mantém apenas uma única posição aberta a qualquer momento. A cada novo dia de negociação, ele recalcula a grade dinâmica a partir dos valores máximo, mínimo e próximo do dia anterior. Esses níveis orientam os gatilhos de entrada, stop-loss, take-profit e gerenciamento opcional de trailing.

## Lógica de negociação

1. **Cálculo de pivô**
   - Na primeira vela de cada novo dia de negociação, a estratégia agrega a máxima, a mínima e o fechamento do dia anterior para calcular os níveis clássicos de pivô do trader de pregão (Pivot, R1–R3, S1–S3).
   - Uma entrada de log é produzida sempre que novos níveis são gerados para que você possa acompanhar como a grade evolui.

2. **Condições de entrada**
   - Em cada vela horária concluída, a estratégia verifica as duas últimas velas concluídas.
   - Uma posição *curta* é aberta quando a vela de dois períodos atrás atingiu um pico acima de um nível de resistência (ou fechou em/acima dele) enquanto abria abaixo dele, e a vela mais recente fechou abaixo desse nível. Isso indica um rompimento com falha e espera uma reversão para baixo.
   - Uma posição *longa* é aberta simetricamente quando o mercado cai abaixo de um nível de suporte, mas a vela seguinte fecha acima dele.
   - Apenas uma posição pode estar ativa por vez. O volume do pedido é definido pelo parâmetro `OrderVolume`.

3. **Gerenciamento de saídas**
   - Cada entrada usa os níveis de stop-loss e take-profit definidos pela predefinição `TargetMode` selecionada. As predefinições refletem as opções `TgtProfit` do consultor especialista original e combinam diferentes níveis de pivô:
     | Modo | Entrada curta | Parada Curta | Alvo curto | Entrada longa | Longa parada | Alvo longo |
     |------|-------------|------------|--------------|------------|-----------|-------------|
     | 1    | R1          | R2         | S1           | S1         | S2        | R1          |
     | 2    | R1          | R2         | S2           | S1         | S2        | R2          |
     | 3    | R2          | R3         | S1           | S2         | S3        | R1          |
     | 4    | R2          | R3         | S2           | S2         | S3        | R2          |
     | 5    | R2          | R3         | S3           | S2         | S3        | R3          |
   - Se `IntradayTrading` estiver ativado, qualquer posição aberta será fechada no fechamento da vela das 23:00 para evitar a manutenção durante a noite.
   - Um trailing stop opcional em pontos (múltiplos da etapa de preço do instrumento) emula o comportamento MetaTrader. O trailing é ativado somente após o movimento ter avançado pela distância configurada e fecha a negociação quando o preço retrocede na mesma quantidade.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `OrderVolume` | Volume usado para ordens de compra e venda a mercado. |
| `TargetMode` | Número inteiro de 1 a 5 selecionando qual combinação de resistência/suporte é usada para entradas, paradas e alvos. |
| `TrailingPoints` | Distância do trailing stop medida em faixas de preço. Defina como zero para desativar o rastreamento. |
| `IntradayTrading` | Quando `true`, as posições são fechadas às 23h para continuar negociando intradiário. |
| `CandleType` | Tipo de dados vela. O padrão é o período de uma hora para corresponder ao especialista original. |

## Notas

- A estratégia espera um fluxo contínuo de velas horárias. Aplicá-lo a outros intervalos de tempo altera o comportamento e deve ser testado novamente.
- Os níveis de stop-loss e take-profit são avaliados usando extremos de velas, portanto, as lacunas entre os níveis podem resultar em saídas com preços piores, assim como na versão MetaTrader.
- O gerenciamento de rastreamento é realizado em fechamentos de velas e mínimos/máximos, correspondendo de perto à lógica original baseada em ticks, permanecendo eficiente no ambiente StockSharp.
