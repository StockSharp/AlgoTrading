# Estratégia AFStar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia AFStar rastreia mudanças de momentum de curto prazo combinando uma ampla
gama de cruzamentos de EMA rápida/lenta com um filtro de rompimento de canal baseado em
Williams %R. Somente quando ambos os componentes concordam é que a estratégia gera sinais
acionáveis.

Uma seta de compra é produzida quando pelo menos uma EMA rápida (dentro do intervalo
configurado) cruza acima de uma EMA lenta compatível enquanto o oscilador baseado em
Williams %R escapa da banda inferior após permanecer dentro da zona neutra. Uma seta de
venda é gerada pelas condições simétricas para cruzamentos de baixa e uma saída da banda
superior. Os sinais são executados após o número configurado de barras definido pelo
parâmetro **Signal Bar**, assim como no especialista MetaTrader original.

Uma vez que uma posição está aberta, a estratégia pode opcionalmente anexar níveis
protetores de stop loss e take profit expressos em passos de preço. Essas proteções são
verificadas em cada candle fechado. Todas as negociações usam o parâmetro constante
**Order Volume**, de modo que as regras complexas de gestão monetária da versão MQL5 são
substituídas por uma abordagem de tamanho fixo mais simples.

## Lógica de entrada

- **Comprado:**
  - Pelo menos uma EMA rápida dentro de `[Start Fast, End Fast]` sobe acima de uma EMA
    lenta dentro de `[Start Slow, End Slow]` usando o incremento `Step Period`.
  - O canal Williams %R, avaliado com valores de risco no intervalo `[Start Risk, End Risk]`
    e `Risk Step`, detecta uma ruptura acima do limite superior após permanecer dentro da
    banda neutra.
  - Posições vendidas opcionais são fechadas previamente quando **Enable Sell Exits**
    está ativado.
- **Vendido:**
  - Cruzamento simétrico e rompimento de Williams %R na direção oposta.
  - Saídas longas opcionais ocorrem primeiro quando **Enable Buy Exits** está habilitado.

## Lógica de saída

- Setas opostas fecham posições quando as flags de saída correspondentes estão habilitadas
  (setas de compra fecham posições vendidas, setas de venda fecham compradas).
- Níveis opcionais de stop loss e take profit medidos em passos de preço podem fechar
  posições mais cedo se o preço atingir esses limites.

## Parâmetros

- **Order Volume** – tamanho da negociação usado para ordens de mercado.
- **Candle Type** – período para dados de mercado (padrão: candles de 4 horas).
- **Start Fast / End Fast / Step Period** – intervalo de EMA rápida para varredura de cruzamentos.
- **Start Slow / End Slow** – intervalo de EMA lenta emparelhado com os valores de EMA rápida.
- **Start Risk / End Risk / Risk Step** – limites da varredura de risco de Williams %R.
- **Signal Bar** – número de barras concluídas a aguardar antes de executar um sinal.
- **Stop Loss (pips)** – distância opcional de stop loss em passos de preço.
- **Take Profit (pips)** – distância opcional de take profit em passos de preço.
- **Enable Buy Entries / Enable Sell Entries** – permitir entradas compradas ou vendidas.
- **Enable Buy Exits / Enable Sell Exits** – habilitar fechamento na direção oposta.

## Notas

- A estratégia mantém até 512 candles recentes para avaliar a lógica AFStar.
- Se os passos de preço não estiverem disponíveis para o ativo, o valor 1 é usado ao
  calcular as distâncias de stop-loss e take-profit.
- Os sinais são enfileirados de modo que **Signal Bar = 0** executa imediatamente,
  enquanto valores maiores atrasam a execução por esse número de barras concluídas.
