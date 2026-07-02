# Estratégia do Painel Peter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Peter Panel** transporta o painel de controle discricionário MetaTrader 5 "Peter Panel" para StockSharp. O consultor especialista original desenhou três linhas horizontais (entrada, take-profit e stop-loss) e uma matriz de botões que permite ao trader enviar instantaneamente ordens de mercado ou pendentes usando esses níveis. Essa estratégia C# mantém o fluxo de decisão intacto enquanto substitui o painel gráfico por parâmetros de estratégia interativos. Cada alternância se comporta como o botão original: definir o parâmetro como `true` executa a ação imediatamente e o sinalizador é redefinido para `false`.

## Conceitos-chave

1. **Assistente manual** – a estratégia não gera sinais. Você decide quando negociar alternando os parâmetros expostos na UI da estratégia ou nos scripts de automação.
2. **Linhas de preço compartilhadas** – a linha de entrada aqua, a linha verde de take-profit e a linha vermelha de stop-loss são representadas por três parâmetros decimais. Seus valores podem ser definidos manualmente ou recalculados em torno do preço médio atual por meio do botão de alternância `ResetCommand`.
3. **Cobertura abrangente de pedidos** – todos os seis tipos de pedidos do painel são implementados: compra/venda de mercado, stop de compra, limite de compra, stop de venda e limite de venda. As ordens de proteção são anexadas após cada preenchimento, emulando os campos TP/SL que o painel MetaTrader preencheu automaticamente.
4. **Modificações em massa** – o parâmetro `ModifyCommand` reaplica as linhas de preço atuais a cada ordem pendente ativa e às ordens protetoras de stop-loss/take-profit da posição aberta.
5. **Liquidação com um toque** – o botão `CloseCommand` cancela ordens pendentes pendentes, remove ordens de proteção e nivela a posição líquida no mercado.

## Implementação original vs. StockSharp

| Recurso | MetaTrader Painel 5 Pedro | StockSharp Estratégia do Painel Peter |
| --- | --- | --- |
| Interface do usuário | Diálogo no gráfico com botões e campos editáveis | Parâmetros de estratégia que se comportam como interruptores e entradas numéricas |
| Manipulação de entrada/TP/SL | Arraste as linhas horizontais ou pressione "Redefinir" para centralizar novamente | Edite os valores dos parâmetros diretamente ou use o botão de alternância `ResetCommand` |
| Envio de pedido | Botão aciona solicitação síncrona `OrderSend` | A alternância de parâmetros chama o auxiliar `Buy/Sell` correspondente e armazena referências de pedidos |
| Tratamento TP/SL | Preenchido por meio de `MqlTradeRequest.tp` e `.sl` em todos os pedidos | A parada de proteção e o alvo são registrados como ordens de parada/limite separadas imediatamente após o preenchimento |
| Modificação do pedido | Selecione um ticket na lista e pressione "Modificar" | `ModifyCommand` cancela/substitui todos os pedidos pendentes ativos e atualiza ordens de proteção |
| Fechamento de pedido | Pressione "Fechar" no ticket destacado | `CloseCommand` fecha toda a posição e cancela todas as ordens pendentes e protetoras |
| Lista de pedidos | Tabela gráfica de tickets e níveis | A estratégia depende do rastreamento de pedidos StockSharp; o status detalhado está disponível nos logs |

> **Observação:** MetaTrader permitiu que o comerciante selecionasse um único bilhete da lista. A porta StockSharp aplica modificações e fechamentos a cada pedido criado pela estratégia porque uma seleção direta de ticket único não está disponível dentro dos parâmetros da estratégia.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `Volume` | Volume de negociação em lotes. Ele é validado em relação à etapa do volume de segurança e aos limites mínimo/máximo. |
| `EntryLevel` | Preço utilizado para ordens pendentes (linha aqua). |
| `TakeProfitLevel` | Preço da linha verde. Ele atua como nível de lucro para negociações longas e como nível de parada protetora para negociações curtas, refletindo o painel original. |
| `StopLossLevel` | Preço da linha vermelha. Ele atua como parada protetora para negociações longas e como meta de lucro para negociações curtas. |
| `BuyMarketCommand` | Envie uma ordem de compra a mercado quando definido como `true`. A sinalização é redefinida para `false` após o pedido ser enviado. |
| `BuyStopCommand` | Coloque uma ordem de compra stop em `EntryLevel`. |
| `BuyLimitCommand` | Faça um pedido com limite de compra em `EntryLevel`. |
| `SellMarketCommand` | Envie uma ordem de venda a mercado. |
| `SellStopCommand` | Coloque uma ordem stop de venda em `EntryLevel`. |
| `SellLimitCommand` | Faça um pedido com limite de venda em `EntryLevel`. |
| `ModifyCommand` | Aplique novamente `EntryLevel`, `TakeProfitLevel` e `StopLossLevel` às ordens pendentes existentes e às ordens de proteção da posição atual. |
| `CloseCommand` | Cancele ordens pendentes, remova ordens de proteção e estabilize a posição no mercado. |
| `ResetCommand` | Recalcular os três níveis de preços em torno do ponto médio atual de compra/venda. |

## Fluxo de trabalho

1. Inicie a estratégia assim que a segurança e o portfólio desejados estiverem conectados. A assinatura de nível 1 atualiza o cache interno de compra/venda que alimenta a função `ResetCommand`.
2. Use a alternância `ResetCommand` ou as edições manuais para configurar os níveis de preço água, verde e vermelho.
3. Acione uma negociação alternando um dos parâmetros de ação para `true`. A estratégia redefine automaticamente a alternância para `false` para que a próxima ativação seja intencional.
4. Após os preenchimentos, a estratégia envia as ordens de stop-loss e take-profit apropriadas com base na direção da posição. Por exemplo, uma posição longa recebe um stop de venda na linha vermelha e um limite de venda na linha verde, enquanto uma posição curta recebe a combinação inversa.
5. Modifique os níveis a qualquer momento e pressione `ModifyCommand` para atualizar ordens pendentes e saídas de proteção sem reiniciar a estratégia.
6. Quando a sessão de negociação terminar, alterne `CloseCommand` para nivelar e limpar todas as ordens gerenciadas pela estratégia.

## Diferenças do painel original

- Não há lista gráfica de tickets. Em vez disso, os registros StockSharp rastreiam cada pedido e negociação registrada. Você pode conectar a estratégia a qualquer UI externa se for necessário o gerenciamento de tickets individuais.
- Os valores de stop-loss e take-profit são implementados como pedidos filhos explícitos porque StockSharp não pode incorporar preços TP/SL diretamente na solicitação de pedido principal. O comportamento corresponde ao resultado final do painel MetaTrader: a posição acaba protegida pelos mesmos níveis.
- A substituição de pedidos é realizada por meio de ciclos de cancelamento e recriação. Isso mantém o fluxo de trabalho determinístico mesmo em locais que não suportam modificações no local.

## Dicas de uso

- Combine a estratégia com StockSharp gráficos ou painéis para recriar a experiência original do painel, substituindo os botões no gráfico por elementos de UI que alternam os parâmetros expostos.
- A estratégia não enfileira múltiplas ações. Se você precisar automatizar sequências (por exemplo, redefinir níveis e, em seguida, colocar um pedido pendente), acione as alternâncias sequencialmente após a anterior ser redefinida para `false`.
- As ordens de proteção são criadas apenas para posições diferentes de zero. Se você colocar ordens pendentes sem posição, ligue para `ModifyCommand` depois que a ordem for preenchida para garantir que os níveis mais recentes sejam aplicados.

## Considerações de segurança

- Sempre verifique se as informações do portfólio, do título e da etapa de preço estão disponíveis antes de enviar qualquer pedido. A estratégia registra avisos quando faltam dados necessários.
- O parâmetro `Volume` está limitado aos limites do instrumento. Se o volume ajustado chegar a zero devido a uma etapa ou volume mínimo incompatível, nenhum pedido será enviado e um aviso aparecerá no log.
- Quando `CloseCommand` é executado, a estratégia primeiro cancela as ordens de proteção, depois as ordens pendentes e, finalmente, nivela a posição. Isto reflete a ordem defensiva das operações do consultor especialista original.
