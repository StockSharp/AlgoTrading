# Estratégia de níveis de Stoch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Níveis Stoch** é uma conversão direta do MetaTrader 4 consultor especialista `Stoch.mq4`. O script original depende dos limites diários da sessão, calcula níveis de preços personalizados da vela anterior e coloca duas ordens pendentes para a próxima sessão. Esta versão C# mantém a mesma ideia comercial e a implementa com a estratégia de alto nível de StockSharp API.

A estratégia calcula uma faixa de negociação sintética expandindo o spread máximo/mínimo da vela anterior por um multiplicador configurável (padrão `1.1`). Em seguida, posiciona:

- Uma ordem de **limite de venda** acima do fechamento anterior na metade da faixa expandida.
- Uma ordem de **limite de compra** abaixo do fechamento anterior na metade da faixa expandida.

Sempre que uma ordem pendente é preenchida, a estratégia anexa imediatamente saídas de colchetes (stop-loss e take-profit) usando as distâncias definidas nas etapas de preço. Todas as exposições pendentes e ordens pendentes são compensadas no início de cada novo dia de negociação, refletindo o bloco de redefinição à meia-noite do script MQL.

## Lógica de negociação
1. Assine a série de velas configurada (diariamente por padrão) e aguarde as velas totalmente finalizadas.
2. Quando uma nova sessão chega:
   - Feche qualquer posição aberta e cancele todas as ordens de proteção ou de entrada.
   - Calcule o intervalo expandido `range * RangeMultiplier` usando a vela anterior.
   - Faça novos pedidos com limite de venda e compra em `Close + range / 2` e `Close - range / 2` respectivamente.
3. Ao preencher o pedido, crie pedidos correspondentes de stop-loss e take-profit usando as compensações de etapas de preço solicitadas.
4. Se uma das ordens de proteção for acionada, cancele a ordem de proteção irmã e aguarde a próxima redefinição da sessão.

## Parâmetros
| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `TakeProfitPoints` | Distância de lucro medida em etapas de preço. | `20` | Equivalente à entrada `TakeProfit` no script MQL. Defina como `0` para desativar a ordem de realização de lucro. |
| `StopLossPoints` | Distância de stop-loss medida em etapas de preço. | `40` | Equivalente à entrada `StopLoss` no script MQL. Defina como `0` para desativar a ordem de stop-loss. |
| `RangeMultiplier` | Multiplicador aplicado ao intervalo de velas anterior (`High - Low`). | `1.1` | Corresponde ao fator de expansão `1.1` codificado em MQL. |
| `OrderVolume` | Volume para cada ordem pendente. | `1` | Espelha o parâmetro `Lots`. |
| `CandleType` | Série de velas que define o pregão. | `Daily` | Personalize se a estratégia deve operar em outros prazos. |

Todos os parâmetros são configurados via `Param()` para oferecer suporte à otimização e vinculação da IU.

## Gestão de risco
- As entradas longas recebem um suporte protetor de **sell stop** e **sell limit**; shorts obtêm saídas espelhadas de **buy stop** e **buy limit**.
- Os pedidos são dimensionados usando `OrderVolume`. Quando um lado do colchete é executado, a ordem de proteção restante é cancelada para evitar saídas duplicadas.
- Uma reinicialização completa ocorre em cada nova vela, garantindo que a estratégia não leve exposição além da sessão atual.

## Notas de conversão
- A implementação MQL usou variáveis globais MetaTrader para evitar pedidos duplicados; a versão C# rastreia a última sessão processada internamente (`_lastProcessedDay`).
- O ciclo de fechamento noturno foi traduzido no auxiliar `ResetOrders()` que cancela todas as ordens pendentes e envia um comando de achatamento de mercado se uma posição permanecer.
- Os níveis de stop-loss e take-profit são recriados explicitamente por meio de métodos de pedido StockSharp em vez de serem incorporados em parâmetros `OrderSend`.
- Trailing stop, gerenciamento de dinheiro e entradas de risco presentes no script MQL não foram utilizados lá e permanecem sem suporte nesta porta.

## Dicas de uso
1. Anexe a estratégia a um título e defina `OrderVolume`, distâncias de stop e tipo de vela para corresponder ao instrumento negociado.
2. Certifique-se de que a segurança exponha um `PriceStep` adequado; caso contrário, a estratégia volta para `1` e registra um aviso.
3. Como os pedidos são recalculados apenas uma vez por vela concluída, mantenha o prazo diário padrão para alinhar com o comportamento original.
4. Revise os registros para confirmar a redefinição diária, a colocação de pedidos e o fluxo de trabalho de anexação de pedidos de proteção.

## Arquivos
- `CS/StochLevelsStrategy.cs` – implementação da estratégia principal.
- `README.md`, `README_zh.md`, `README_ru.md` – documentação multilíngue para a estratégia convertida.
