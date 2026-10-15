# Estratégia ABE BE RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia ABE BE RSI** é uma versão do MetaTrader consultor especialista `Expert_ABE_BE_RSI`. O sistema combina padrões clássicos de reversão de velas com confirmação de impulso do Índice de Força Relativa (RSI). Duas velas consecutivas devem formar um padrão envolvente de alta ou baixa, e a vela concluída mais recentemente deve mostrar uma leitura RSI dentro dos limites predefinidos. Regras cruzadas RSI adicionais são aplicadas para nivelar ou reverter posições existentes, refletindo de perto a lógica de decisão da implementação MQL original.

## Lógica de negociação

1. **Detecção de padrões envolventes**
A estratégia avalia as duas últimas velas concluídas. Um sinal de alta requer:
   - A vela *t-2* fecha mais baixo do que abre (corpo de baixa).
   - A vela *t-1* fecha mais alto do que abre (corpo de alta).
   - O tamanho do corpo da vela *t-1* excede a média móvel dos tamanhos de corpo recentes (o padrão é cinco barras).
   - A vela *t-1* fecha acima da abertura da vela *t-2* e abre abaixo do seu fechamento, garantindo um verdadeiro evento envolvente.
   - O ponto médio da vela *t-2* está abaixo da média móvel dos preços de fechamento, confirmando uma tendência de baixa de curto prazo.

Um sinal de absorção de baixa usa as condições simétricas: a vela mais antiga é de alta, a vela mais recente é de baixa com um corpo maior que a média e a vela mais recente engole totalmente o corpo anterior enquanto o ponto médio da barra mais antiga fica acima da média móvel para confirmar um esgotamento da tendência de baixa.

2. **RSI Confirmação**
   - As entradas longas exigem que o RSI da vela fechada mais recentemente esteja abaixo do nível de entrada de alta configurado (padrão 40).
   - As entradas curtas exigem que o RSI da vela fechada mais recentemente esteja acima do nível de entrada de baixa (padrão 60).

3. **Gerenciamento de saídas**
RSI cruzamentos em dois níveis são monitorados para fechar posições existentes:
   - As posições curtas são cobertas quando RSI sobe acima do limite de saída inferior (padrão 30) ou superior (padrão 70) após estar abaixo dele na vela anterior.
   - As posições longas são fechadas quando RSI cai abaixo de qualquer limite após ter estado acima dele na vela anterior.

4. **Execução de pedido**
As ordens de mercado são usadas tanto para entradas quanto para saídas. Ao reverter, a estratégia primeiro fecha a exposição atual e depois entra na nova direção com o volume base configurado. O dimensionamento de posição imita o modelo de lote fixo do especialista MQL.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Volume` | Tamanho do pedido em contratos. | `0.1` |
| `RsiPeriod` | Número de barras usadas pelo filtro RSI. | `11` |
| `MovingAveragePeriod` | Período para o tamanho do corpo da vela e médias móveis do preço de fechamento. | `5` |
| `BullishEntryLevel` | Valor máximo de RSI que ainda valida uma entrada envolvente de alta. | `40` |
| `BearishEntryLevel` | Valor mínimo de RSI necessário para uma entrada de baixa. | `60` |
| `ExitLowerLevel` | Nível de cruzamento inferior RSI para posições planas. | `30` |
| `ExitUpperLevel` | Nível de cruzamento superior RSI para posições planas. | `70` |
| `CandleType` | Série de velas processada pela estratégia. | `1 hour time frame` |

Todos os parâmetros podem ser otimizados no Designer ou Runner graças aos wrappers `StrategyParam`.

## Pipeline de Indicadores

- **Índice de Força Relativa (RSI)** – calcula o impulso sobre o `RsiPeriod` configurável e fornece limites de entrada/saída.
- **Média Móvel Simples de preços de fechamento** – fornece um contexto de tendência usado para validar padrões envolventes.
- **Média móvel simples dos tamanhos do corpo da vela** – garante que a vela envolvente seja maior que o tamanho médio do corpo nas últimas `MovingAveragePeriod` barras.

## Notas de uso

- A estratégia atua apenas em velas totalmente concluídas (`CandleStates.Finished`). Os dados parciais da barra são ignorados para evitar sinais prematuros.
- O histórico de velas é armazenado internamente para avaliar condições de engolfamento sem percorrer grandes coleções, respeitando as diretrizes de conversão de todo o projeto.
- `StartProtection()` é ativado para que os mecanismos de proteção base StockSharp se tornem ativos quando a exposição da posição for diferente de zero.

## Diferenças em relação ao Expert Advisor original

- O Expert Advisor original depende do sistema de votação por sinal de MetaTrader. Neste porto, os votos são traduzidos em ações diretas de entrada e saída que replicam as mesmas condições.
- O gerenciamento de dinheiro é simplificado para um único parâmetro `Volume`, refletindo o tamanho fixo do lote (`Money_FixLot_Lots`) usado pelo especialista de origem.
- O suporte para trailing-stop não está incluído, pois a versão MT5 usava um módulo "no trailing".

## Testes recomendados

1. Anexe a estratégia a um gráfico no Designer ou API Runner com um símbolo que reage historicamente a reversões envolventes (por exemplo, principais pares de FX).
2. Verifique RSI e os parâmetros de média móvel antes de executar sessões ao vivo; os padrões reproduzem as configurações publicadas do Expert Advisor.
3. Use os recursos de otimização integrados para explorar RSI limites alternativos ou períodos médios para diferentes mercados.
