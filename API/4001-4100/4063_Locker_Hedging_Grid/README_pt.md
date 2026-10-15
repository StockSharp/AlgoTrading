# Estratégia de grade de hedge de armário
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica o consultor especialista MetaTrader 4 **Locker.mq4**. Ele inicia cada ciclo com uma compra no mercado e depois gerencia uma grade protegida de ordens de compra e venda. Sempre que o lucro não realizado combinado de todas as negociações abertas atinge uma fração fixa do patrimônio da conta, todas as posições são fechadas e um novo ciclo começa. Se a perda flutuante exceder a mesma fração na direção negativa, a estratégia adiciona progressivamente ordens de resgate em intervalos de pontos fixos, bloqueando as oscilações de preços com entradas alternadas de compra e venda.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `NeedProfitRatio` | Fração do patrimônio do portfólio que deve ser ganha (ou perdida) antes de fechar/adicionar ordens. `0.001` corresponde a 0,1% da conta. | `0.001` |
| `InitialVolume` | Volume da primeira ordem de compra de mercado no início de cada ciclo. | `0.5` |
| `StepVolume` | Volume para cada ordem de resgate que é adicionada enquanto a estratégia está em fase de rebaixamento. | `0.2` |
| `StepPoints` | Distância em MetaTrader pontos entre ordens de resgate. Convertido internamente em preço usando informações de `Security.PriceStep` (pip). | `50` |
| `EnableRescue` | Ativa a grade de média quando a perda flutuante ultrapassa o limite negativo. Se desabilitada, a estratégia realiza apenas a negociação inicial e aguarda o lucro. | `true` |

## Lógica de negociação

1. **Início do ciclo**
   - Na primeira cotação recebida, uma compra de mercado é enviada com `InitialVolume`.
   - O preço de entrada se torna o ponto de verificação de referência, e os rastreadores de compra mais alta e de venda mais baixa são redefinidos para esse preço.

2. **Bloqueio de lucro**
   - A cada tick, a estratégia soma os lucros e perdas não realizados de todas as pernas longas e curtas. Pernas longas contribuem com `(price - averageBuyPrice) * longVolume`, enquanto pernas curtas adicionam `(averageSellPrice - price) * shortVolume`.
   - Assim que o lucro flutuante atingir `NeedProfitRatio * equity`, todas as posições serão achatadas por meio de ordens de mercado opostas. Um novo ciclo é iniciado após a confirmação dos preenchimentos.

3. **Grade de resgate**
   - Quando o lucro não realizado cai abaixo de `-NeedProfitRatio * equity` e `EnableRescue` é verdadeiro, o sistema espera que o preço se mova `StepPoints` (convertido em distância do preço). Cada nova máxima acima do último ponto de verificação gera outra compra no mercado, enquanto cada nova mínima programa uma venda no mercado. Os volumes são sempre iguais a `StepVolume`.
   - Os pontos de verificação e os extremos direcionais são atualizados após cada ordem de resgate, de modo que a próxima adição exija outro passo completo no preço.

4. **Reinicialização do ciclo**
   - Depois que os estoques longos e curtos caírem para zero (confirmado por meio de notificações de negociação próprias), o ponto de verificação e os extremos serão redefinidos para o preço de negociação mais recente e a estratégia estará pronta para semear um novo ciclo com a compra inicial.

## Notas de implementação

- Usa `SubscribeTrades().Bind(ProcessTrade)` para trabalhar com preços tick-by-tick, espelhando o MQL EA original que reagiu ao lance/pergunta atual.
- Converte MetaTrader "pontos" em StockSharp preços por meio de um tamanho de pip derivado de `Security.PriceStep`. Os símbolos citados com 3 ou 5 casas decimais recebem o ajuste padrão *x10*.
- Rastreia os estoques longos e vendidos separadamente dentro de `OnOwnTradeReceived`, permitindo a exposição coberta exatamente como a versão MT4 (posições de compra e venda podem coexistir).
- O patrimônio do portfólio é estimado em `Portfolio.CurrentValue` com substitutos para `CurrentBalance` ou `BeginValue`. A primeira leitura positiva é armazenada em cache para que o limite de lucro permaneça estável mesmo que o provedor pare de reportar o valor.
- Cada volume de ordem de mercado passa por um auxiliar `AlignVolume` que respeita as restrições `Security.VolumeStep`, `VolumeMin` e `VolumeMax`.

## Dicas de uso

- Certifique-se de que os metadados do instrumento forneçam um `PriceStep` correto; caso contrário, a conversão ponto-preço será imprecisa e as distâncias da grade não corresponderão ao comportamento MetaTrader.
- Como a lógica de resgate reflete uma média no estilo martingale, escolha `StepVolume` com cuidado e monitore o risco. Aumentar `StepPoints` e `StepVolume` reduz o número de negociações abertas, mas amplifica a exposição.
- Defina `EnableRescue` como `false` para replicar uma variante conservadora que simplesmente espera que a primeira posição atinja a meta de lucro sem nunca diminuir a média.
- O backtesting em símbolos Forex deve ser realizado com dados de ticks para corresponder à granularidade original do EA.

## Diferenças do especialista MQL

- O script original tentou fechar pares de ordens perfeitamente compensadores quando mais de oito negociações estavam ativas. Esse bloco nunca foi executado devido a um bug no filtro de tickets e foi omitido.
- O recálculo `StepLot` baseado em pedidos pré-existentes na inicialização não é replicado; os volumes são controlados inteiramente através dos parâmetros expostos em StockSharp.
- Comentários de pedidos, pop-ups de alerta e sinalizadores de parada manual do EA não estão presentes – a versão do StockSharp concentra-se puramente na lógica de negociação autônoma.
