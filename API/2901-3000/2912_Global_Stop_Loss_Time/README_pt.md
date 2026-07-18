# Estratégia de Stop Loss Global e Janela de Negociação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o comportamento do especialista MetaTrader **Exp_GStopLoss_Tm**, fornecendo uma camada de risco sobreposta que monitora o resultado combinado de todas as operações abertas pela instância da estratégia. O módulo não gera sinais de entrada por si próprio; em vez disso, rastreia o lucro e perda das posições existentes e aplica tanto um limite de stop loss global quanto uma janela de sessão de negociação opcional. Quando as perdas excedem o limite configurado ou o mercado se move fora do intervalo de tempo permitido, a estratégia liquida a exposição atual e bloqueia qualquer operação posterior até que o livro esteja novamente plano.

## Lógica de negociação
1. Na inicialização, a estratégia registra o PnL realizado atual como referência base. Isso permite medir o lucro flutuante relativo ao estado plano mais recente.
2. Cada vela concluída produzida pelo tipo de vela configurado aciona uma verificação de risco. O período padrão é um minuto para emular vigilância em nível de tick sem sobrecarregar o sistema.
3. O módulo calcula o lucro não realizado como a diferença entre o PnL atual da estratégia e o valor base. PnL positivo é ignorado enquanto a estratégia permanece dentro da janela de negociação, espelhando o consultor especialista original.
4. Se o modo de perda estiver definido como **Percent**, a estratégia compara o percentual de perda absoluta com o patrimônio da conta obtido de `Portfolio.CurrentValue`. Para o modo **Currency**, a comparação é feita em unidades de moeda absolutas.
5. Uma vez que o limite de perda seja superado, o sinalizador de stop é travado e a estratégia começa a fechar a posição aberta na próxima iteração. O sinalizador só é liberado após o tamanho da posição retornar a zero e o PnL base ser atualizado.
6. Quando a janela de negociação opcional está habilitada, a verificação de risco também avalia se o tempo de fechamento da vela está dentro do intervalo permitido. A janela suporta sessões intradiárias que se estendem à meia-noite, espelhando a lógica do MetaTrader.
7. Sempre que o sinalizador de stop estiver ativo ou o filtro de sessão detectar que o mercado está fora do horário permitido, o módulo envia uma ordem de mercado na direção oposta para achatar a posição. Entradas de log informativas descrevem o motivo de cada saída.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| `LossMode` | Seleciona como o limite de perda é interpretado: porcentagem do patrimônio atual da conta ou moeda absoluta da conta. |
| `StopLoss` | Valor do limite de perda. Para o modo porcentagem o número representa o percentual, enquanto o modo moeda usa a moeda da conta. |
| `UseTimeFilter` | Habilita a janela de negociação intradiária. Quando desabilitado, a estratégia ignora o filtro de tempo completamente. |
| `StartTime` | Início inclusivo da janela de negociação em UTC. Funciona junto com `EndTime` para definir a sessão válida. |
| `EndTime` | Fim exclusivo da janela de negociação em UTC. Suporta sessões wrap-around quando o tempo de fim é anterior ao de início. |
| `CandleType` | Assinatura de velas usada para conduzir a avaliação de risco periódica. O padrão é um período de 1 minuto. |

## Notas de implementação
- O PnL base é recalculado sempre que o tamanho da posição retorna a zero, de modo que as operações subsequentes comecem com uma base limpa.
- Os valores de patrimônio são obtidos do portfólio ao vivo, portanto o modo percentual se adapta tanto a mudanças realizadas quanto não realizadas no valor da conta.
- Todos os comentários no código-fonte estão escritos em inglês, conforme exigido pelas convenções do projeto.
- A estratégia desenha velas e operações próprias na área do gráfico padrão quando uma está disponível, ajudando a visualizar o comportamento durante os testes.

## Diretrizes de uso
1. Anexe a estratégia ao instrumento que deseja supervisionar. A geração de ordens de outras estratégias ainda pode ocorrer; este módulo apenas monitora e fecha posições.
2. Configure o modo de perda e o limite que corresponda ao seu apetite de risco. Por exemplo, `LossMode = Percent` e `StopLoss = 5` fecharão a posição após uma queda não realizada de 5% em relação ao patrimônio atual.
3. Defina os parâmetros `StartTime` e `EndTime` para limitar o trading a uma sessão intradiária específica. Para cobrir uma janela noturna, especifique um horário de início posterior ao horário de fim (por exemplo, 20:00 a 06:00).
4. Execute o backtest ou a sessão ao vivo. A estratégia reiniciará automaticamente o sinalizador de stop assim que todas as posições estiverem achatadas e continuará supervisionando as operações subsequentes.
