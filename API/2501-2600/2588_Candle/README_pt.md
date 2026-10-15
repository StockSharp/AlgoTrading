# Estratégia de Candle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Candle** é um port direto do clássico expert MT5 "Candle.mq5". Avalia a cor de cada candle concluído no timeframe selecionado e mantém a posição alinhada com o fechamento mais recente. Candles de alta levam a estratégia para comprado, candles de baixa para vendido, e candles planos deixam a posição inalterada. O risco é controlado por distâncias de take-profit e trailing stop em pips que são convertidas a preços absolutos através do tamanho de tick do instrumento.

A estratégia só reage após um candle estar completamente formado para evitar ruído dentro da barra. Um lookback obrigatório (`MinBars * 2` candles concluídos) valida que o gráfico contém histórico suficiente, enquanto um período de esfriamento configurável aguarda entre operações. Isso produz uma implementação fiel em StockSharp da lógica MetaTrader original sem depender de acesso a séries de baixo nível.

## Lógica de trading
### Preparação
- Processa candles fornecidos por `CandleType`; nenhuma outra fonte de dados é necessária.
- Aguarda até que pelo menos `2 * MinBars` candles concluídos tenham sido processados antes de permitir entradas.
- Opera apenas quando a estratégia está online, formada e tem permissão para executar ordens.
- Aplica o intervalo `TradeCooldown` (padrão: 10 segundos) entre quaisquer duas operações.

### Regras de entrada e reversão
1. **Estado plano:**
   - Entrar comprado (`BuyMarket`) quando um candle fecha acima de sua abertura.
   - Entrar vendido (`SellMarket`) quando um candle fecha abaixo de sua abertura.
2. **Posição existente:**
   - Se uma posição comprada enfrenta um candle de baixa, vender `|Position| + Volume` para fechar e imediatamente reverter para uma posição vendida de tamanho `Volume`.
   - Se uma posição vendida enfrenta um candle de alta, comprar `|Position| + Volume` para fechar e imediatamente reverter para uma posição comprada de tamanho `Volume`.
3. **Candles neutros:**
   - Quando o fechamento é igual à abertura, nenhuma ação manual é tomada; apenas as ordens protetoras podem sair da operação.

### Gestão de risco e saídas
- `StartProtection` anexa um take-profit e um trailing stop medidos em pips. A estratégia multiplica cada valor de pip por `(PriceStep * 10)` para corresponder ao ajuste do MetaTrader para cotações de 3 e 5 dígitos.
- O trailing stop é ativado apenas quando `TrailingStopPips` é maior que zero; segue o preço automaticamente uma vez que a operação se move na direção favorável.
- O take-profit fecha a posição quando a distância configurada é atingida. Qualquer nível protetor cancela a ordem oposta após a execução.
- Reversões manuais causadas pela cor do candle também liquidam a exposição anterior antes de abrir a nova posição.

## Parâmetros
- `CandleType` – período do candle da série a analisar (padrão: candles de 15 minutos).
- `TakeProfitPips` – distância ao alvo de take-profit em pips (padrão: 50).
- `TrailingStopPips` – distância do trailing stop em pips (padrão: 30).
- `MinBars` – contagem mínima de barras necessária antes da primeira operação (padrão: 26; a estratégia aguarda 52 candles concluídos).
- `TradeCooldown` – período de espera após qualquer ação de trading (padrão: 10 segundos).

Defina a propriedade `Volume` da estratégia com o tamanho de ordem desejado. Quando o mercado reverte, a estratégia envia automaticamente volume suficiente tanto para sair da posição anterior quanto para estabelecer a nova.

## Notas de implementação
- Apenas candles concluídos (`CandleStates.Finished`) são processados. Isso reflete o expert MetaTrader, que dependia de valores de barra fechada obtidos via `CopyOpen/CopyClose`.
- O código usa a API de alto nível do StockSharp: `SubscribeCandles` para dados, `Bind` para processar barras entrantes e `BuyMarket`/`SellMarket` para execução de ordens.
- Ordens protetoras são gerenciadas por `StartProtection`, portanto não é necessário manter registro manual de ordens stop-limit.
- O cálculo do tamanho do pip `PriceStep * 10` reproduz a lógica "digits adjust" do MQL para símbolos cotados com 3 ou 5 casas decimais.
- Como as entradas são acionadas pelo corpo do candle mais recente, a estratégia tende a permanecer no mercado continuamente, alternando lados sempre que a cor do candle muda.

Ajuste as distâncias em pips, o período de esfriamento e o timeframe para corresponder ao instrumento sendo negociado. A configuração padrão reflete a amostra MT5 original, mas pode ser otimizada através do framework de parâmetros do StockSharp.
