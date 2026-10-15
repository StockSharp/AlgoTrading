# Exemplo de estratégia Trailingstop MT5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O **SampleTrailingstopMt5Strategy** reproduz o comportamento do MetaTrader 5 consultor especialista `SampleTrailingstop-MT5.mq5` usando o alto nível de StockSharp API. A estratégia mantém constantemente ordens de breakout stop emparelhadas, protege posições preenchidas com ordens de saída dedicadas e aplica um trailing stop quando a negociação se torna lucrativa. Todos os cálculos baseiam-se na etapa de preço do instrumento para que a lógica corresponda à implementação original baseada em “pontos”.

## Lógica de negociação
1. **Feed de dados**. A estratégia assina cotações de nível 1 para receber os melhores preços de compra/venda que orientam a ordem e as atualizações de trailing stop.
2. **Pedidos de entrada**.
   - Uma ordem stop de compra é colocada acima do mercado atual usando `BuyStop`. O pedido é atualizado somente quando a instância anterior é concluída.
   - Uma ordem stop de venda reflete a entrada comprada usando `SellStop` abaixo do mercado.
   - Ambas as ordens de entrada compartilham o mesmo volume configurável, distâncias de stop-loss e de take-profit. Os pedidos também recebem um prazo de validade com um dia de antecedência, correspondendo à implementação de MQL.
3. **Proteção de posição**.
   - Após os preenchimentos, a estratégia rastreia a posição líquida assinada e o preço médio de entrada.
   - Ordens de saída stop e take-profit separadas são criadas (`SellStop`/`BuyStop` e `SellLimit`/`BuyLimit`) para que os níveis de proteção permaneçam na bolsa mesmo se as ordens de entrada forem canceladas ou expirarem.
   - As ordens de saída são continuamente sincronizadas com o tamanho da posição atual e o preço médio de entrada mais recente.
4. **Lógica final**.
   - Quando o lucro flutuante atinge a distância móvel configurada, o stop de proteção é reduzido para manter essa distância da oferta atual (para posições compradas) ou da venda (para posições vendidas).
   - O trailing stop nunca cruza o preço de entrada e respeita um incremento mínimo de atualização igual a uma etapa de preço.
5. **Rastreamento de posição**. Cada negociação própria atualiza o valor da posição acumulada e recalcula o preço médio ponderado de entrada para que preenchimentos parciais e reversões sejam processados ​​corretamente.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradeVolume` | Volume de ordem fixa (lotes ou contratos) usado para ambas as ordens stop de rompimento. |
| `TakeProfitPoints` | Distância em pontos do instrumento para a meta de lucro. Defina como zero para desativar o take-profit. |
| `StopLossPoints` | Distância em pontos para o stop loss de proteção. |
| `TrailingStopPoints` | Distância final em pontos aplicada quando a posição é lucrativa. Zero desativa o rastreamento. |

## Notas comportamentais
- As ordens de entrada só serão reenviadas após o término da instância anterior (preenchida, cancelada ou expirada). Isso reflete a lógica `CheckPendingOrder` do especialista original.
- As distâncias stop-loss e take-profit são sempre convertidas em valores de preço usando `Security.PriceStep`, garantindo um comportamento consistente em diferentes instrumentos.
- Se a posição for totalmente fechada, a estratégia cancela automaticamente todas as ordens de saída restantes e zera as médias internas.
- A estratégia depende exclusivamente de dados de nível 1 e não requer velas ou indicadores, mantendo a conversão próxima ao modelo MQL.

## Uso
1. Atribua o título e o portfólio desejados antes de iniciar a estratégia.
2. Ajuste os quatro parâmetros públicos para alinhar com o instrumento negociado (volume, stop-loss, take-profit e trailing distance).
3. Lance a estratégia. Ele gerenciará de forma autônoma as ordens de fuga e a proteção de posições em tempo real.
