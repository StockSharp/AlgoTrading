# Estratégia de filtro de faixa de investidores Up3x1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma porta direta do consultor especialista MetaTrader 4 **up3x1_Investor**. Ele negocia um único instrumento usando velas concluídas em um período configurável (H1 por padrão). A porta replica a lógica original com StockSharp APIs de alto nível e adiciona parâmetros claros de gerenciamento de risco.

## Lógica de negociação
- A estratégia avalia a última vela totalmente fechada e verifica se:
  - A faixa da vela (máxima menos mínima) excede `0.0060` unidades de preço.
  - O corpo da vela (diferença absoluta entre abertura e fechamento) excede `0.0050` unidades de preço.
- Se a vela fechar em alta e as condições acima forem atendidas, a estratégia abre uma posição de mercado **longa**.
- Se a vela fechar em baixa e as condições forem atendidas, a estratégia abre uma posição de mercado **curta**.
- A negociação é completamente desativada às segundas-feiras (para espelhar a proteção `DayOfWeek()==1` do código MQL).

## Gerenciamento de posição
- Após a entrada, a estratégia define metas internas usando as distâncias configuradas baseadas em etapas:
  - `TakeProfitPoints` → distância até a meta de lucro.
  - `StopLossPoints` → distância de parada protetora.
  - `TrailingStopPoints` → distância usada para rastrear o stop quando o preço se move a favor.
- Stops e alvos são avaliados em cada vela finalizada:
  - Se o preço atingir o alvo, a posição será fechada ao preço alvo.
  - Se o preço atingir o stop, a posição será fechada para limitar a perda.
  - Uma vez que o preço avança além da distância de fuga, o stop é movido para mais perto do preço de mercado para garantir o lucro.
- Além disso, se as médias móveis simples de 24 e 60 períodos calculadas nas mesmas velas se tornarem iguais (dentro de uma etapa de preço), a posição será fechada imediatamente. Isso imita a lógica MQL em que o pedido é fechado quando ambas as médias correspondem exatamente.

## Gestão de Volume e Risco
- `BaseVolume` define o tamanho do lote substituto quando nenhum ajuste baseado em conta pode ser calculado.
- `MaximumRisk` replica a fórmula `AccountFreeMargin()*MaximumRisk/1000` original. Se o valor do portfólio estiver disponível, a estratégia dimensiona a posição como `value * MaximumRisk / 1000`, arredondada para uma casa decimal.
- `DecreaseFactor` imita a redução da sequência de perdas: após mais de uma perda consecutiva, o volume diminui proporcionalmente para `losses / DecreaseFactor`.
- `MinimumVolume` garante que o volume nunca caia abaixo do menor tamanho negociável usado no script MQL (0,1 lote).

## Parâmetros
| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `BaseVolume` | `0.1` | Tamanho base da posição em lotes quando nenhum ajuste de risco é aplicado. |
| `MaximumRisk` | `0.2` | Fator de risco usado para derivar o volume do patrimônio da conta (igual ao EA original). |
| `DecreaseFactor` | `3` | Reduz o tamanho da posição após perdas consecutivas. |
| `MinimumVolume` | `0.1` | Menor volume permitido. |
| `TakeProfitPoints` | `20` | Distância da meta de lucro medida em etapas de preço. |
| `StopLossPoints` | `50` | Distância de stop-loss medida em etapas de preço. |
| `TrailingStopPoints` | `10` | Distância do trailing stop medida em etapas de preço. |
| `SkipMondays` | `true` | Desative todas as atividades de negociação às segundas-feiras. |
| `CandleType` | `1 hour` | Prazo para assinatura da vela. |

## Notas
- A estratégia mantém apenas uma posição aberta por vez, correspondendo à guarda `CalculateCurrentOrders` original.
- O rastreamento de perdas consecutivas é puramente interno porque os corretores StockSharp não expõem o histórico de pedidos de MetaTrader.
- Nenhuma ordem pendente é usada; todas as negociações são enviadas como ordens de mercado via `BuyMarket` e `SellMarket`.
