# Estratégia ENewsLuckyw
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **ENewsLuckyw Strategy** é um sistema de breakout baseado em tempo convertido do consultor especialista MetaTrader *e-News-Lucky$*. Em um horário programado, ele envia ordens de compra e venda em torno do preço atual, recentrando-as continuamente enquanto ambas as ordens estão ativas e realiza o gerenciamento de posição que imita a lógica MQL original. Saídas protetoras, trilhas opcionais e uma limpeza no final do dia completam o fluxo de trabalho.

## Lógica de negociação
- **Colocação straddle programada.** Em `SetOrdersTime` a estratégia cancela quaisquer ordens pendentes restantes, mede o fechamento atual da vela e coloca ordens de stop simétricas a `DistancePips` do preço de mercado.
- **Atualização contínua de ordens.** Quando ambas as ordens pendentes estão ativas, elas são realinhadas em cada vela finalizada, mantendo o straddle centrado no preço, como o especialista original fez em cada nova barra.
- **Preparação de entrada.** Os níveis de stop-loss e take-profit opcionais são pré-calculados para que possam ser anexados imediatamente quando uma posição é aberta. As ordens pendentes opostas são removidas assim que uma posição aparece.
- **Proteção contra rastreamento.** Se `UseTrailing` estiver ativado, a ordem de stop se moverá em `TrailingStopPips` sempre que a posição tiver avançado em pelo menos `TrailingStepPips`. Com `ProfitTrailing` ativado, o rastreamento começa somente depois que o lucro excede a distância final, replicando a opção MQL "ProfitTrailing".
- **Limpeza de sessão.** Em `DeleteOrdersTime` todas as ordens pendentes são canceladas e qualquer posição aberta é fechada para evitar riscos durante a noite.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `Volume` | Volume de ordens em lotes utilizados para ambas as ordens stop. |
| `StopLossPips` | Distância de parada protetora. Zero desativa a parada. |
| `TakeProfitPips` | Distância de lucro opcional. Zero desativa o alvo. |
| `DistancePips` | Compensação do preço atual para as ordens stop de rompimento. |
| `UseTrailing` | Permite parar o trailing quando a posição estiver aberta. |
| `ProfitTrailing` | Exige que o lucro não realizado exceda a distância final antes de mover o stop. |
| `TrailingStopPips` | Distância entre o preço e o trailing stop. |
| `TrailingStepPips` | Melhoria mínima necessária antes que o trailing stop seja atualizado novamente. |
| `SetOrdersTime` | Hora do dia em que o straddle é colocado. |
| `DeleteOrdersTime` | Hora do dia para cancelamento de ordens e fechamento de posições. |
| `CandleType` | Assinatura de velas usada para controle de tempo e manutenção de pedidos. |

## Notas de uso
1. Anexe a estratégia ao instrumento desejado e configure `CandleType` para corresponder ao tamanho da barra que deseja usar para manutenção (o padrão são velas de 1 minuto).
2. Defina os parâmetros de programação para alinhá-los com seu evento de notícias ou sessão de negociação.
3. Ajustar distâncias e controles de risco de acordo com a volatilidade do instrumento. Para símbolos Forex, certifique-se de que a etapa de preço esteja configurada corretamente para que `StopLossPips`, `TakeProfitPips` e `DistancePips` se traduzam nas compensações de preço esperadas.
4. O sistema de trailing usa ordens de stop e limite para saídas. Se o seu local não oferece suporte a esses tipos de ordens, substitua-as por saídas de mercado ou ordens simuladas antes de entrar no ar.
5. A estratégia realiza uma redefinição diária por data. Se você executá-lo à meia-noite no fuso horário da bolsa, certifique-se de que a sessão de negociação se estende em um único dia de negociação.

## Notas de conversão
- A estratégia reflete o fluxo de trabalho do especialista MQL: colocação programada (`SetOrders`), manutenção de hora em hora (`ModifyOrders`), remoção de pedidos pendentes conflitantes (`DeleteOppositeOrders`), lógica de rastreamento (`TrailingPositions`) e limpeza no final do dia.
- Os cálculos de preços com reconhecimento de spread do código MQL são aproximados usando o fechamento da última vela porque StockSharp normaliza os preços para o `PriceStep` do instrumento.
- Todas as configurações de som, número de conta e cor do script original foram omitidas porque não têm equivalente no API de alto nível de StockSharp.
