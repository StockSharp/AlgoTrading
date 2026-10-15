# Estratégia de fuga noturna Pipso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Pipso é um sistema de breakout de sessão noturna convertido do MetaTrader consultor especialista `Pipso.mq4`. A estratégia mede o
preços mais altos e mais baixos das velas previamente concluídas e reage quando o mercado sai dessa faixa. Cada
o rompimento inverte a posição: as posições longas são fechadas e uma posição curta é aberta quando o preço ultrapassa as máximas recentes, enquanto
as posições curtas são cobertas e uma nova compra é estabelecida quando o preço ultrapassa os mínimos recentes. As paradas de proteção são derivadas de
a largura do intervalo para que a distância de parada se adapte automaticamente à volatilidade atual.

## Como funciona
1. Assine o prazo configurado (15 minutos por padrão) e aguarde os indicadores construírem um histórico completo.
2. Para cada nova vela finalizada, calcule a máxima mais alta e a mínima mais baixa das velas `BreakoutPeriod` anteriores. O atual
vela não faz parte desse intervalo, exatamente como no EA original, onde `iHighest(..., shift = 1)` pula a barra de trabalho.
3. Recalcule a distância de parada como `(high - low) * StopLossMultiplier` enquanto aplica a distância mínima definida por
`MinStopDistance`.
4. Mantenha uma janela de negociação definida por `SessionStartHour` e `SessionLengthHours`. Quando a janela passa da meia-noite de sexta-feira
é prorrogado por dois dias para que as negociações abertas sobrevivam ao fim de semana, assim como em MetaTrader.
5. Quando a máxima da vela excede a máxima de fuga armazenada:
   - Feche qualquer posição longa existente e, se a negociação for permitida, abra uma posição curta com tamanho `OrderVolume`.
   - Anexe um stop loss acima do preço de entrada usando a distância de stop calculada.
6. Quando a mínima da vela cai abaixo da mínima de fuga armazenada:
   - Feche qualquer posição curta existente e, se a negociação for permitida, abra uma posição longa com tamanho `OrderVolume`.
   - Anexe um stop loss abaixo do preço de entrada usando a distância de stop calculada.
7. As paradas de proteção são avaliadas em cada vela acabada. Se a baixa tocar a parada longa ou a alta atingir a parada curta,
a posição é achatada imediatamente.

## Lógica da Sessão de Negociação
- `SessionStartHour` é expresso em horas de troca. O comprimento da janela é definido com `SessionLengthHours`.
- Se a sessão se estender além de 24 horas e o dia atual for sexta-feira, o final da janela será adiantado em 48 horas para que
que a negociação recomeça na segunda-feira, correspondendo ao tratamento do fim de semana no código MQL4.
- Fora da janela de negociação a estratégia apenas fecha as posições existentes; novas negociações são permitidas novamente assim que a janela for aberta.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Tipo de dados Candle usado para cálculo de sinal. | Período de 15 minutos |
| `OrderVolume` | Tamanho fixo da ordem para cada ordem de mercado. | 1 |
| `SessionStartHour` | Hora do dia em que a janela de breakout começa. | 21 |
| `SessionLengthHours` | Duração da janela de negociação em horas. | 9 |
| `BreakoutPeriod` | Número de velas concluídas que definem o intervalo de rompimento. | 36 |
| `StopLossMultiplier` | Multiplicador aplicado à largura do intervalo para derivar a distância de parada (o valor `3` corresponde ao `SLpp = 300` original). | 3 |
| `MinStopDistance` | Distância mínima de stop-loss em unidades de preço absoluto, emulando a restrição de nível de stop MetaTrader. | 0 |

## Notas
- A estratégia utiliza apenas ordens de mercado; não há lucro. O stop-loss protetor é o único mecanismo de saída além
o sinal de fuga oposto.
- Ao mudar de compra para venda (ou vice-versa), a estratégia envia uma única ordem de mercado que fecha a anterior.
posição e abre a nova, espelhando o comportamento da fonte EA que chamou sequencialmente `OrderClose` e
`OrderSend`.
- As linhas indicadoras para os máximos e mínimos de ruptura são traçadas automaticamente no gráfico de estratégia juntamente com as negociações executadas.
