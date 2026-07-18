# Estratégia Maybeawo222
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Maybeawo222 replica o consultor especialista MetaTrader "maybeawo222" usando o StockSharp de alto nível de API. Ele negocia um único instrumento com um cruzamento de média móvel simples (SMA) na vela anterior e limita a atividade a uma janela de tempo configurável. A conversão mantém o gerenciamento de equilíbrio encenado que tenta travar os lucros assim que o preço avança em distâncias predefinidas.

## Lógica de negociação
1. A estratégia assina a série principal de velas selecionada por meio de `CandleType` e calcula uma média móvel simples com o período especificado por `MovingPeriod`.
2. No fechamento de cada vela, o valor de SMA é deslocado em `MovingShift` barras antes de ser usado na decisão. Isso reproduz a chamada `iMA` original com um parâmetro shift.
3. Os sinais de negociação são avaliados apenas quando o horário de fechamento da vela finalizada está dentro da faixa `[StartHour, EndHour)`. Fora dessa janela não são criadas novas ordens, embora as posições abertas continuem a ser geridas.
4. Um sinal de **compra** aparece quando a vela anterior (aquela que acabou de fechar) abre abaixo do SMA deslocado e fecha acima dele. Um sinal de **venda** requer o cruzamento oposto. A estratégia reverte as posições existentes, se necessário, para que apenas uma direção permaneça aberta.
5. Em cada vela finalizada, o mecanismo verifica os extremos máximo/mínimo para detectar acertos de stop-loss ou take-profit. Sempre que qualquer um dos níveis é atingido, a saída do mercado correspondente é acionada imediatamente.
6. A posição também ativa até dois ajustes de equilíbrio em estágios. Quando o lucro flutuante excede `BreakevenPips1`, o stop se aproxima da entrada de acordo com `DesiredBreakevenDistancePips1`. Uma segunda etapa repete o processo com `BreakevenPips2` e `DesiredBreakevenDistancePips2`.

## Gestão de risco
- As distâncias iniciais de stop-loss e take-profit são configuradas em pips. A conversão usa o instrumento `PriceStep` e aplica o fator convencional MetaTrader de 10 para cotações de três e cinco dígitos.
- Os níveis de equilíbrio são aplicados apenas uma vez por lado da posição. Cada nova entrada redefine os sinalizadores, permitindo que o stop seja seguido duas vezes durante a vida da negociação.
- As saídas de posição usam ordens de mercado para que o mecanismo possa fechar negociações mesmo que os níveis de stop ou alvo não estejam disponíveis no lado da corretora.

## Parâmetros
| Nome | Padrão | Faixa/Notas | Descrição |
|------|---------|---------------|-------------|
| `MovingPeriod` | `14` | Inteiro positivo | Comprimento SMA usado para a verificação cruzada. |
| `MovingShift` | `0` | `0` – `10` (sugerido) | Número de velas concluídas para deslocar o valor SMA para trás. |
| `StopLossPips` | `100` | `0` desativa | Distância do preço de entrada ao stop loss de proteção, medida em pips. |
| `TakeProfitPips` | `800` | `0` desativa | Distância da entrada ao nível de take-profit, medida em pips. |
| `BreakevenPips1` | `180` | `0` desativa | Limite de lucro (em pips) que aciona o primeiro ajuste de equilíbrio. |
| `DesiredBreakevenDistancePips1` | `60` | Qualquer não negativo | Nova distância de parada a partir da entrada após o estágio 1 de equilíbrio ser acionado. |
| `BreakevenPips2` | `500` | `0` desativa | Limite de lucro (em pips) que aciona o segundo ajuste de equilíbrio. |
| `DesiredBreakevenDistancePips2` | `350` | Qualquer não negativo | Nova distância de parada a partir da entrada após o estágio de equilíbrio 2 disparar. |
| `StartHour` | `3` | `0` – `23` | Hora de início da sessão de negociação inclusiva, com base no horário da bolsa. |
| `EndHour` | `22` | `0` – `23` | Horário de término exclusivo do pregão. |
| `OrderVolume` | `0.5` | Maior que `0` | Volume enviado com cada ordem de mercado antes da compensação de posições. |
| `CandleType` | `H1` | Qualquer tipo de dados de vela | Série de velas usada para gerar sinais e calcular o SMA. |

## Notas para uso
- Certifique-se de que a segurança conectada forneça um `PriceStep` válido; caso contrário, a conversão do pip volta para `1`. Ajuste os parâmetros relacionados ao pip de acordo se o seu instrumento cotar em ticks grandes.
- A estratégia espera uma configuração de símbolo único. Adicione-o a um esquema com o instrumento desejado antes de iniciar a estratégia.
- Para negociação em tempo real, considere permitir permissões de derrapagem ou ordens de stop de proteção através de extensões específicas da corretora se as saídas do mercado não forem suficientes.
