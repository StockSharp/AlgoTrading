# Estratégia Martin Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia reproduz o comportamento do Consultor Especializado original "Martin" do MQL executando uma grade martingale com cobertura em torno do preço atual. Ela alterna continuamente posições compradas e vendidas, dobrando o volume negociado a cada reversão até que o lucro acumulado de toda a cesta atinja o alvo configurado. Os candles são usados apenas como motor para a lógica de decisão, enquanto as execuções reais dependem de ordens a mercado e ordens stop expostas pela API de alto nível do StockSharp.

## Como funciona
1. Na inicialização, a estratégia lê o `PriceStep` do instrumento para converter os parâmetros `EntryOffsetPoints` e `StepPoints` em distâncias de preço absolutas. Se o passo de preço não estiver disponível, o valor 1 é assumido.
2. Quando não há posição aberta nem ciclo martingale ativo, a estratégia coloca uma ordem stop de compra e uma ordem stop de venda em torno do último fechamento. Os offsets são `EntryOffsetPoints * PriceStep`, o que coincide com a distância de 10 pontos usada no código MQL original.
3. Quando uma das ordens stop é executada, a ordem pendente oposta é cancelada. A execução define o primeiro trade da sequência martingale: a estratégia armazena seu preço, volume e direção, e define o contador de nível interno como 1.
4. A cada fechamento de candle subsequente, o preço de fechamento atual é comparado ao preço da última ordem executada. Se o mercado se moveu contra essa ordem pelo menos `martingaleLevel * StepPoints * PriceStep`, uma ordem a mercado é enviada na direção oposta com volume dobrado em relação ao trade anterior. As informações do último trade são atualizadas após cada execução.
5. O lucro não realizado é avaliado como `PnL + Position * (closePrice - PositionPrice)`. Quando esse lucro agregado supera o parâmetro `ProfitTarget`, a estratégia envia `CloseAll()` para nivelar cada posição na cesta, cancela todas as ordens restantes e reinicia o ciclo para que um novo par de ordens stop possa ser colocado.
6. O mesmo reinício também ocorre automaticamente quando todas as posições são fechadas manualmente: os contadores internos são limpos e novas ordens stop serão criadas no próximo candle.

Este fluxo de trabalho espelha a lógica de compra/venda alternada do Consultor Especializado original enquanto mantém a implementação totalmente dentro da API de alto nível do StockSharp.

## Parâmetros
- `StepPoints` – número de passos de preço usados para calcular o limite de reversão para a próxima ordem de média. Padrão 10 e pode ser otimizado.
- `EntryOffsetPoints` – offset para as ordens stop iniciais de compra/venda em passos de preço. Também padrão 10 pontos como a versão MQL.
- `ProfitTarget` – lucro absoluto em moeda necessário para fechar toda a cesta martingale. Uma vez que o PnL combinado realizado e não realizado supera esse valor, todas as posições são liquidadas.
- `CandleType` – assinatura de candles usada para impulsionar a lógica da estratégia. O padrão é o período de um minuto, mas qualquer `DataType` suportado pelo local pode ser selecionado.

O tamanho base do trade é retirado da propriedade `Volume` da estratégia. Cada nova reversão multiplica essa base por potências de dois da maneira martingale clássica.

## Notas práticas
- Sempre configure `Volume` para corresponder ao tamanho mínimo de lote do broker. O esquema de duplicação aumenta rapidamente a exposição, portanto os limites de risco devem ser aplicados externamente.
- Como a colocação de ordens é impulsionada por fechamentos de candles, movimentos de preço rápidos dentro do candle podem acionar entradas ligeiramente mais tarde do que a versão MQL baseada em ticks. No entanto, as ordens stop mantêm os preços de entrada alinhados com a lógica original.
- A estratégia desenha candles de preço e trades próprios na área de gráfico padrão para rastreamento visual mais fácil.
- Nenhum stop-loss automático é usado. A única condição de saída é o `ProfitTarget`, portanto o instrumento e o período devem ser escolhidos cuidadosamente para controlar o risco de grandes tendências adversas.

## Diferenças em relação ao Especialista MQL
- O StockSharp usa posições líquidas, portanto cada reversão é executada com uma ordem a mercado que fecha a exposição anterior e abre a nova em um único trade. O PnL cumulativo da cesta permanece idêntico à implementação com cobertura.
- A lógica tick a tick foi substituída por fechamentos de candles para avaliação de sinais, a fim de permanecer dentro do uso recomendado da API de alto nível.
- Os identificadores de ordens são rastreados para evitar processar execuções parciais múltiplas vezes, garantindo que a lógica de duplicação de volume permaneça consistente.

Essas mudanças mantêm o comportamento de negociação fiel à estratégia fonte enquanto a adaptam ao framework do StockSharp.
