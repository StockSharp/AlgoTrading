# Rompimento Bollinger com ciclo de ordens por sessão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O diagrama transforma um rompimento Bollinger bilateral em um ciclo visível de ordens pendentes. Um fechamento concluído fora de Bollinger Bands(20, 1) registra entrada na banda rompida; a execução cria limite oposto na média móvel, Order replacing acompanha essa linha e o fim da sessão 07:00-20:00 cancela tudo e zera a posição.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de cinco minutos alimentam Bollinger Bands com período 20 e largura 1, além do close usado nas duas comparações.
- Working time aceita novas entradas somente entre 07:00 e 20:00, e a trava Position == 0 torna o diagrama propositalmente flat-only.
- Rompimento superior confirmado registra compra limitada na banda superior; o inferior registra venda espelhada na banda inferior.
- O Trade.Volume real da entrada dimensiona a saída oposta na média, sem trocar execução parcial ou não padrão por uma constante.
- Combination guarda a ordem mais recente de Order replacing; uma saída executada ou o fim do horário remove todas as ordens vivas restantes.

## Regras de entrada e saída

- **Entrada comprada**: Dentro de Working time, close finalizado acima da banda superior com Position == 0 registra compra limitada no valor dessa banda.
- **Entrada vendida**: Dentro de Working time, close finalizado abaixo da banda inferior com Position == 0 registra venda limitada no valor dessa banda.
- **Saída**: Após a entrada executar, registra-se na média Bollinger um limite oposto pelo volume exato e ele é movido a cada novo valor. Sua execução cancela sobras. Fora de 07:00-20:00 todas as ordens são canceladas e Modify position fecha long ou short a mercado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo finalizado do diagrama, adaptado das quatro horas do C# para replay mensal. |
| Bollinger Period | 20 | Length das bandas Bollinger e parâmetro C# real. |
| Bollinger Width | 1 | Multiplicador de desvio padrão Bollinger e parâmetro C# real. |
| Session Start | 07:00:00 | Início de Working time vindo do README, não do construtor C#. |
| Session End | 20:00:00 | Fim de Working time; fora dele ordens são canceladas e a posição zerada. |
| Order Volume | 1 | Volume de cada entrada; a saída usa o volume realmente executado. |

## Detalhes do diagrama

- O C# executável usa candles de quatro horas. Cinco minutos é adaptação replay explícita para gerar valores formados e rompimentos suficientes no mês de aceitação.
- Só BandPeriod, BandWidth e CandleType são parâmetros do construtor C#. Session Start e Session End vêm do README e são implementados por Working time; o volume também é exposto.
- C# aceita Position <= 0 para long e Position >= 0 para short, fechando e revertendo a posição contrária a mercado. Este exemplo entra apenas com Position == 0 e não reverte.
- A execução também muda: a fonte entra e sai a mercado; o diagrama põe a entrada na banda rompida e mantém limite de saída na média. A entrada pode esperar um recuo.
- Order replacing devolve novo objeto de ordem. As ordens inicial e substituída de cada lado convergem em Combination<Order>, sem realimentação própria.
- O ajuste ao passo de preço fica desativado porque o instrumento replay não informa esse passo; os níveis do indicador permanecem iguais.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
