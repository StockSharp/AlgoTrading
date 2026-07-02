# Estratégia de Monitoramento de Risco
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Risk Monitor Strategy é uma versão do MetaTrader 4 consultor especialista `risk.mq4`. O script original nunca abriu negociações; em vez disso
determinou quantos lotes o trader poderia implantar com segurança com base no saldo da conta e em uma porcentagem de risco definida pelo usuário. Isto
A versão StockSharp mantém o mesmo espírito: realiza diagnósticos contínuos da conta, calcula tamanhos de negociação sugeridos, monitora
lucros flutuantes e realizados e publica os resultados diretamente no comentário da estratégia para uma tomada de decisão rápida.

Ao contrário das estratégias convencionais, a Risk Monitor Strategy não envia pedidos automaticamente. A sua função é de supervisão: dá ao
trader um instantâneo da exposição atual, capacidade disponível de acordo com o orçamento de risco escolhido e a rentabilidade do fechado
posições. A linha de comentários é atualizada sempre que as posições, PnL ou negociações mudam, para que as informações sempre reflitam as últimas
estado do portfólio.

## Cálculos
A estratégia deriva os números exibidos no comentário de três grupos de dados:

1. **Tamanho do lote base** – calculado como `AccountBalance / 1000` e alinhado à etapa de volume de segurança. Isso reflete o original
Lógica MT4 onde cada 1000 unidades de saldo correspondem a 1 lote padrão.
2. **Tamanho do lote de risco** – multiplica os lotes base por `Risk % / 100`, alinha o resultado à etapa de volume e representa quantos
os lotes poderão ser abertos respeitando o orçamento de risco configurado.
3. **Lotes em aberto e diferença** – compara a posição líquida absoluta com o tamanho do lote de risco. Se o comerciante estiver abaixo do limite,
a diferença mostra quantos lotes restam disponíveis antes de atingir o limite. Uma pequena diferença negativa que é menor que
o passo do volume é arredondado para zero para evitar ruídos confusos.

Para lucros, a estratégia distingue entre valores flutuantes e realizados:

* **PnL flutuante** – lido da propriedade da estratégia `PnL` e expresso em unidades de preço e como porcentagem do valor atual
valor do portfólio.
* **Lucro realizado** – acumulado em negociações próprias. O componente divide cada preenchimento de fechamento em partes positivas e negativas,
aplica a comissão informada e mantém um total corrente. O valor final também é convertido em um percentual do patrimônio líquido para
corresponder à leitura MT4.

## Parâmetros
* **% de risco** – parcela do saldo da conta que pode ser comprometida em novas posições. Padrão: `10`. O parâmetro é exposto para
otimização para que diferentes orçamentos de risco possam ser testados rapidamente.

## Formato de comentário
A estratégia atualiza o comentário com três linhas:

1. `Base lots`, `Risk lots`, `Open lots`, `Lots to adjust` – visualização rápida das métricas de dimensionamento de posição.
2. `Risk`, `Floating PnL` – configuração de risco, lucro flutuante em unidades monetárias e lucro flutuante em porcentagem do saldo.
3. `Realized profit` – lucro fechado acumulado e sua porcentagem.

Todos os valores são arredondados de forma semelhante ao script MT4, respeitando a etapa do lote de segurança e utilizando duas casas decimais para valores monetários
números. Como a saída fica no comentário, ela fica imediatamente visível no gráfico ou na grade de estratégia sem abrir
painéis adicionais.

## Notas de uso
* Anexe a estratégia ao instrumento cujo equilíbrio e posição você deseja supervisionar. Funciona com posições líquidas (não estilo MT4
hedge) assim como o próprio StockSharp.
* A estratégia tolera a negociação manual: ela reage a qualquer confirmação de negociação para manter as estatísticas sincronizadas.
* O comentário é limpo automaticamente quando a estratégia é interrompida ou redefinida, evitando que valores obsoletos persistam nas sessões.
* Nenhuma implementação Python é fornecida; o pacote API contém apenas a versão C#.
