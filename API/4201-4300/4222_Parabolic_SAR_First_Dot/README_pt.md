# Parabolic SAR Estratégia do primeiro ponto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Parabolic SAR First Dot Strategy** é a StockSharp conversão de alto nível do MetaTrader consultor especialista `pSAR_bug_4` da pasta `MQL/9954`. O sistema reage ao primeiro ponto do Parabolic SAR que aparece no lado oposto do preço. Quando o SAR cai abaixo do fechamento, uma negociação longa é aberta; quando o SAR salta acima do fechamento, uma negociação curta é executada. Cada posição é protegida com distâncias fixas de stop-loss e take-profit expressas em Parabolic SAR "pontos", assim como na versão original MQL.

## Lógica de negociação
1. **Preparação de dados e indicadores**. A estratégia assina um tipo de vela configurável (velas de 15 minutos por padrão) e vincula um indicador Parabolic SAR com etapa de aceleração definida pelo usuário e aceleração máxima.
2. **Rastreamento de estado**. Na primeira vela concluída, a estratégia lembra se o SAR está acima ou abaixo do fechamento. As velas posteriores comparam a nova posição SAR com o estado anterior.
3. **Regras de entrada**.
   - **Entrada longa**: o SAR muda de cima para baixo do fechamento. Qualquer posição curta existente é fechada e uma nova posição longa com o volume configurado é aberta no mercado.
   - **Entrada curta**: o SAR muda de abaixo do fechamento para acima do fechamento. Qualquer posição longa existente é fechada antes de abrir uma nova posição curta.
4. **Ordens de proteção**. Imediatamente após a entrada, a estratégia armazena os níveis de stop-loss e take-profit calculados a partir do fechamento da vela, multiplicando `StopLossPoints` ou `TakeProfitPoints` pelo título `PriceStep`. Se `UseStopMultiplier` estiver ativado (comportamento padrão copiado de MetaTrader), a distância será multiplicada por 10 para contabilizar corretores que cotam com pips fracionários.
5. **Regras de saída**. Em cada vela finalizada, a estratégia verifica a máxima e a mínima em relação aos níveis armazenados de stop-loss e take-profit. Se a máxima ou mínima ultrapassar o nível, a posição é fechada no mercado. Quando um sinal oposto SAR chega, a posição também é revertida enviando uma ordem dimensionada para estabilizar a exposição atual e abrir a nova negociação.

## Gestão de risco
- As distâncias de stop-loss e take-profit são recalculadas para cada nova posição.
- O código executa um substituto conservador: quando o título não fornece uma etapa de preço, um valor de `0.0001` é usado para evitar distâncias zero.
- Todas as decisões de negociação usam o auxiliar `IsFormedAndOnlineAndAllowTrading()` para garantir que a assinatura esteja ativa e ativa.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volume de pedidos utilizado para novas posições. O parâmetro também atualiza a propriedade base `Strategy.Volume`. |
| `StopLossPoints` | `90` | Distância de stop-loss expressa em Parabolic SAR pontos. O valor é multiplicado pela segurança `PriceStep` (e opcionalmente por 10 quando `UseStopMultiplier` for verdadeiro). |
| `TakeProfitPoints` | `20` | Distância de lucro em Parabolic SAR pontos convertidos por meio da etapa de preço. |
| `UseStopMultiplier` | `true` | Se ativado, multiplica as distâncias de stop-loss e take-profit por 10 para imitar a opção `StopMult` do especialista `StopMult`. |
| `SarAccelerationStep` | `0.02` | Fator de aceleração inicial fornecido ao indicador Parabolic SAR. |
| `SarAccelerationMax` | `0.2` | Fator máximo de aceleração para o indicador Parabolic SAR. |
| `CandleType` | `15m time-frame` | Tipo de vela usado para cálculos de indicadores e sinais. |

## Notas sobre a conversão
- MetaTrader ordens stop-loss e take-profit eram ordens de proteção do lado do corretor. StockSharp os reproduz monitorando os máximos e mínimos das velas e enviando saídas de mercado quando os limites são ultrapassados.
- O especialista MetaTrader multiplicou as distâncias de parada por dez sempre que `StopMult` era verdadeiro para melhorar a compatibilidade com corretores que cotavam com pips fracionários. O parâmetro `UseStopMultiplier` implementa o mesmo comportamento.
- A conversão usa API de alto nível de `SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket`) conforme exigido pelas diretrizes do projeto. Nenhuma versão adicional do Python foi fornecida ainda, correspondendo à solicitação da tarefa.
