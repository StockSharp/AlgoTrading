# Estratégia AMS ES RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
A estratégia AMS ES RSI replica o comportamento do MetaTrader especialista `Expert_AMS_ES_RSI` dentro de StockSharp. Ele combina formações de velas estelares da manhã/vespertina com um filtro de confirmação do Índice de Força Relativa (RSI). As negociações longas são abertas quando uma estrela da manhã de alta aparece enquanto RSI indica condições de sobrevenda. As negociações curtas são realizadas quando uma estrela da noite de baixa se forma em conjunto com um RSI sobrecomprado. As posições são fechadas quando RSI ultrapassa os níveis de limite configuráveis.

## Suposições de mercado
- Funciona em qualquer instrumento que produza velas OHLC regulares. Spot FX e futuros de índices foram os alvos originais do especialista MQL.
- A estratégia espera uma ação de preço suave onde os padrões de velas japonesas sejam significativos. Gráficos de ticks extremamente barulhentos podem não produzir sinais confiáveis.

## Lógica de entrada
1. Assine o prazo configurado (padrão: 1 hora) e aguarde três velas totalmente fechadas.
2. Calcule o tamanho médio do corpo nas últimas velas *BodyAveragePeriod* (padrão: 3).
3. Detecte uma **Estrela da Manhã** quando:
   - A vela 3 é fortemente baixista (`Open - Close` maior que o tamanho médio do corpo).
   - A vela 2 tem um corpo real pequeno (menos da metade da média) e lacunas abaixo da vela 3.
   - A vela 1 fecha acima do ponto médio da vela 3.
4. Detecte uma **Evening Star** com condições simétricas de baixa.
5. Confirme entradas longas quando o valor atual de RSI estiver abaixo de *LongEntryRsi* (padrão: 40). Confirme as entradas curtas quando RSI estiver acima de *ShortEntryRsi* (padrão: 60).
6. Execute ordens de mercado usando a estratégia `Volume`.

## Sair da lógica
- Fechar posições longas quando RSI cruzar para baixo através de *UpperExitRsi* (padrão: 70) ou *LowerExitRsi* (padrão: 30).
- Feche as posições curtas quando RSI cruzar para cima através dos mesmos níveis.
- Nenhum stop-loss ou take-profit rígido é aplicado. A gestão de riscos deve ser tratada externamente ou ajustando os limites.

## Parâmetros
| Nome | Descrição | Padrão | Alcance |
| ---- | ----------- | ------- | ----- |
| `CandleType` | Tipo de dados que representa a série de velas a ser assinada. | Período de 1 hora | Qualquer tipo de vela compatível |
| `RsiPeriod` | RSI comprimento de cálculo. | 47 | Otimizável (10–70) |
| `BodyAveragePeriod` | Número de velas usadas para calcular o tamanho médio do corpo necessário para validação do padrão. | 3 | Otimizável (2–6) |
| `LongEntryRsi` | Valor máximo de RSI que permite entradas longas. | 40 | Otimizável (20–50) |
| `ShortEntryRsi` | Valor mínimo de RSI que permite entradas curtas. | 60 | Otimizável (50–80) |
| `LowerExitRsi` | Limite inferior que aciona saídas quando cruzado para cima. | 30 | Otimizável (20–40) |
| `UpperExitRsi` | Limite superior que aciona saídas quando cruzado para baixo. | 70 | Otimizável (60–80) |

## Notas de implementação
- Usa o StockSharp API de alto nível com assinaturas automáticas de velas.
- Baseia-se exclusivamente nos valores dos indicadores fornecidos por `Bind`, evitando chamadas manuais de `GetValue` de acordo com as diretrizes do projeto.
- Mantém apenas um histórico mínimo na memória (três velas recentes) para validação de padrão.
- A estratégia chama automaticamente `StartProtection()` no lançamento para ativar mecanismos de segurança integrados.

## Dicas de uso
1. Anexe a estratégia a um par instrumento/carteira e certifique-se de que a série de velas esteja disponível em seu conector.
2. Ajuste os níveis de RSI de acordo com a volatilidade dos ativos. Limites mais amplos reduzem o número de negociações, mas aumentam a qualidade da confirmação.
3. Combine com módulos externos de dimensionamento de posição (por exemplo, volume baseado em risco) para emular o comportamento do lote fixo do EA original.
4. Ao fazer backtesting, certifique-se de que os dados da vela contenham lacunas para que os padrões estelares possam ser identificados corretamente.
