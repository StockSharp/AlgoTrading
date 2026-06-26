# Estratégia Exp Iin MA Signal MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um port StockSharp do expert MetaTrader "Exp_Iin_MA_Signal_MMRec". A estratégia ouve os sinais de cruzamento produzidos por um par de médias móveis configuráveis (o indicador original Iin_MA_Signal) e aplica um esquema de dimensionamento de posição adaptativo com redução baseada em perdas.

## Visão Geral

- **Geração de sinais**: as médias móveis rápida e lenta são avaliadas no tipo de vela selecionado e no preço aplicado. Um sinal de compra é criado quando a média rápida cruza acima da lenta, enquanto um sinal de venda é produzido no cruzamento oposto. O parâmetro `SignalBar` adia a execução pelo número especificado de barras completamente fechadas, reproduzindo o atraso do buffer do indicador usado na versão MQL.
- **Gerenciamento de posição**: `BuyPosOpen` e `SellPosOpen` habilitam ou desabilitam entradas longas e curtas. Quando um sinal oposto aparece e o flag `BuyPosClose` ou `SellPosClose` correspondente está habilitado, a estratégia fecha a exposição atual ou reverte diretamente para a nova direção.
- **Controle de risco**: `StopLossPoints` e `TakeProfitPoints` são traduzidos para distâncias de preço usando `Security.PriceStep` e verificados contra as extremidades da vela antes de processar novos sinais.
- **Gerenciamento de dinheiro**: as últimas negociações são rastreadas separadamente para compras e vendas. Quando o número de negociações perdedoras dentro da janela `BuyTotalTrigger`/`SellTotalTrigger` atinge o limite de perda respectivo, a estratégia muda de `NormalVolume` para `ReducedVolume`. O parâmetro `MoneyMode` define como o valor do volume é interpretado (lotes fixos, porcentagem do saldo, ou porcentagem de risco baseada em stop).

## Parâmetros

- `FastPeriod`, `SlowPeriod` – comprimentos das médias móveis rápida e lenta.
- `FastType`, `SlowType` – tipos de médias móveis (`Simple`, `Exponential`, `Smoothed`, `Weighted`, `VolumeWeighted`).
- `FastPrice`, `SlowPrice` – preço aplicado para cada média (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`).
- `SignalBar` – número de barras fechadas entre um sinal detectado e o envio da ordem.
- `BuyPosOpen`, `SellPosOpen` – interruptores para abrir posições compradas/vendidas.
- `BuyPosClose`, `SellPosClose` – interruptores para fechar ou reverter uma posição existente no sinal oposto.
- `BuyTotalTrigger`, `SellTotalTrigger` – quantas negociações recentes são inspecionadas para o contador de perdas.
- `BuyLossTrigger`, `SellLossTrigger` – número mínimo de perdas dentro da janela inspecionada que ativa o volume reduzido.
- `NormalVolume`, `ReducedVolume` – volume primário e de fallback (ou fator de risco, dependendo de `MoneyMode`).
- `StopLossPoints`, `TakeProfitPoints` – distâncias de stop loss e take profit em pontos do instrumento.
- `MoneyMode` – interpretação dos valores de volume (`Lot`, `Balance`, `FreeMargin`, `BalanceRisk`, `FreeMarginRisk`). Modos baseados em saldo usam `Portfolio.CurrentValue`, enquanto modos baseados em risco dividem o valor de risco pela distância calculada do stop.
- `CandleType` – série de velas usada para cálculos de indicadores.

## Lógica de Sinais

1. Cada vela terminada alimenta as médias móveis com o preço aplicado escolhido.
2. A diferença entre os valores atuais e anteriores das médias móveis define um evento de cruzamento.
3. Sinais são enfileirados, e a entrada mais antiga é executada assim que o tamanho da fila excede `SignalBar`.
4. Quando um sinal de compra é executado:
   - Se uma posição curta existe e `SellPosClose` está habilitado, a estratégia calcula o PnL realizado para esse trade curto. Em seguida, reverte para um comprado (se `BuyPosOpen` estiver habilitado) ou simplesmente fecha a exposição.
   - Se nenhuma posição está aberta e `BuyPosOpen` está habilitado, um novo comprado é aberto com o volume calculado.
5. Sinais de venda espelham o fluxo de trabalho de compra.

## Detalhes de Gerenciamento de Dinheiro

- O histórico de negociações é armazenado como uma fila FIFO rotativa limitada por `BuyTotalTrigger` / `SellTotalTrigger`.
- Uma negociação perdedora (PnL negativo) incrementa o contador de perdas. Quando o contador atinge `BuyLossTrigger` ou `SellLossTrigger`, a próxima posição usa `ReducedVolume`.
- `MoneyMode = Lot` trata os valores de volume como quantidades brutas.
- `MoneyMode = Balance` e `FreeMargin` multiplicam o valor configurado por `Portfolio.CurrentValue` e dividem pelo preço de fechamento atual para obter a quantidade.
- `MoneyMode = BalanceRisk` e `FreeMarginRisk` multiplicam o valor configurado por `Portfolio.CurrentValue` e dividem pela distância do stop-loss. Se a distância do stop for zero, o fallback é idêntico ao cálculo do porcentagem do saldo.
- Se informações do portfólio não estiverem disponíveis, o volume calculado assume o valor padrão de zero para evitar ordens acidentais.

## Tratamento de Risco

- Os níveis de stop-loss e take-profit são recalculados em cada vela usando o preço de entrada e o valor do ponto. Se um nível for tocado dentro do range da vela, a posição é fechada antes que novos sinais sejam processados.
- As ações de fechamento sempre registram o resultado da negociação, garantindo que as filas de gerenciamento de dinheiro permaneçam sincronizadas com as saídas reais.

## Notas

- Certifique-se de que `StopLossPoints` e `TakeProfitPoints` sejam compatíveis com o tamanho do tick do instrumento; a estratégia os multiplica por `Security.PriceStep`.
- Quando `MoneyMode` depende de dados do portfólio, a estratégia espera que o objeto `Portfolio` exponha `CurrentValue`.
- O algoritmo opera numa base de posição líquida: holdings longos e curtos simultâneos não são suportados.
