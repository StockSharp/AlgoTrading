# Minha estratégia de sistema
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **My System Strategy** é uma porta StockSharp do MetaTrader 4 consultor especialista `MySystem.mq4` (diretório `MQL/9601`). O script original avalia os indicadores Bulls Power e Bears Power, combina seus valores em um sinal de impulso composto e abre posições de estilo de reversão quando o impulso muda de sinal. Esta versão C# reproduz o processo de decisão central, adiciona estado explícito de gerenciamento de risco e expõe cada constante ajustável por meio de parâmetros de estratégia para otimização.

Ao contrário da implementação MQL, que consultou diretamente `iBullsPower`/`iBearsPower` com diferentes preços aplicados em cada barra, a edição StockSharp alimenta ambos os indicadores da série de velas configuradas e rastreia o valor composto anterior internamente. A tradução mantém o período padrão de 15 minutos, as mesmas distâncias de take-profit/stop-loss e as condições de saída finais especificadas no código-fonte.

## Lógica de negociação
1. Assine o fluxo de velas configurado (velas de 15 minutos por padrão) e aguarde as velas totalmente finalizadas.
2. Para cada vela concluída, recupere os valores mais recentes de Bulls Power e Bears Power e calcule sua média `((bulls + bears) / 2)`.
3. Mantenha a média anterior em `_previousAveragePower` para espelhar as chamadas baseadas em turnos em MQL.
4. Regras de entrada (somente quando nenhuma posição estiver aberta):
   - **Entrada curta** – se a média anterior for maior que a média atual e a média atual permanecer positiva. Isso corresponde à condição MQL `pos1pre > pos2cur && pos2cur > 0`.
   - **Entrada longa** – se a média atual ficar negativa (`pos2cur < 0`), significando que o Bears Power domina.
5. O gerenciamento de saída é executado em cada vela, mesmo antes de novos sinais:
   - Avalie os níveis rígidos de take-profit e stop-loss que foram registrados quando a posição foi aberta.
   - Aplique a lógica de trailing stop da fonte EA: para posições compradas, faça o trail out quando o momentum enfraquece (`pos1pre > pos2cur`) e o preço avançou pela distância final; para posições vendidas, saia quando o momentum composto se tornar negativo e o preço tiver se deslocado a favor da distância solicitada.
6. Se um sinal de saída disparar, chame `ClosePosition()` para nivelar; a estratégia então espera pela próxima vela para avaliar novas entradas.

## Parâmetros
| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `TakeProfitPoints` | Distância até o nível de lucro expresso em etapas de preço. | `86` | Espelha a entrada `TakeProfit`. Defina como `0` para desativar a meta de lucro. |
| `StopLossPoints` | Distância até o nível de stop loss expresso em etapas de preço. | `60` | Espelha a entrada `StopLoss`. Defina como `0` para desativar a parada de proteção. |
| `TrailingStopPoints` | Distância usada pela condição de saída final (etapas de preço). | `10` | Quando zero, a lógica final é ignorada. |
| `OrderVolume` | Volume enviado a cada nova entrada. | `8.3` | Corresponde ao parâmetro `Lots` em EA. |
| `PowerPeriod` | Período aplicado aos indicadores Bulls Power e Bears Power. | `13` | Replica o período original. |
| `CandleType` | Série de velas que orienta os cálculos do indicador. | `15m` | Alteração para transferir a estratégia para outro período de tempo. |

Todos os parâmetros são declarados via `Param()` para suportar vinculação de UI e varreduras de otimização.

## Gestão de risco
- Os níveis de proteção são armazenados quando `OnPositionChanged` detecta uma nova exposição longa ou curta. As distâncias são convertidas em preços absolutos usando um auxiliar de tamanho de pip que se aproxima da lógica `Point` de MetaTrader (`PriceStep`, ajustada para símbolos FX decimais de 3/5).
- `ClosePosition()` é invocado quando uma condição de take-profit, stop-loss ou trailing é atendida, garantindo que a estratégia saia com uma única ordem de mercado e evite solicitações de fechamento duplicadas.
- Não são realizados hedges ou fechamentos parciais; a estratégia impõe uma única posição por vez, exatamente como a guarda `OrdersTotal() < 1` no script MQL.

## Notas de conversão
- Os argumentos `PRICE_WEIGHTED` vs `PRICE_CLOSE` de MetaTrader foram aproximados armazenando o valor composto anterior (`pos1pre`) em vez de manter duas instâncias de indicador com feeds de preços diferentes. Isso mantém a intenção comportamental sem duplicar as transformações das velas.
- O EA original continha várias chamadas `OrderSelect` malformadas dentro da lógica final. O porto implementa o efeito pretendido – fechar negociações assim que o preço percorre a distância final enquanto a condição de momentum é satisfeita – de forma determinística.
- As saídas finais são avaliadas em relação aos máximos/mínimos das velas para emular toques intrabarras porque StockSharp processa velas concluídas por padrão.
- O dimensionamento dos pedidos, as distâncias de parada e os períodos dos indicadores mantêm os padrões originais para que as otimizações existentes possam ser reproduzidas sem ajustes.

## Dicas de uso
1. Anexe a estratégia a uma segurança que exponha `PriceStep` e `Decimals`. Se estes estiverem faltando, o auxiliar volta para um tamanho de pip de `1`.
2. Ajuste `OrderVolume`, `TakeProfitPoints` e `StopLossPoints` para alinhar com o tamanho do contrato do instrumento e o valor do tick.
3. Ao testar em intervalos de tempo diferentes, lembre-se de atualizar `CandleType` e considere otimizar novamente a distância final, pois barras mais curtas atingirão o limite com mais frequência.
4. Use gráficos StockSharp (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) para validar que as entradas ocorrem quando o Bulls and Bears Power cruza os limites especificados.

## Arquivos
- `CS/MySystemStrategy.cs` – implementação de estratégia usando StockSharp de alto nível API.
- `README.md`, `README_zh.md`, `README_ru.md` – documentação multilíngue para o consultor especialista convertido.
