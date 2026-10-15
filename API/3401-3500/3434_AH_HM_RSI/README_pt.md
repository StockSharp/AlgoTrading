# AH HM RSI Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do MetaTrader especialista **Expert_AH_HM_RSI**. Ele procura padrões de velas de martelo ou homem enforcado e requer um sinal de confirmação do Índice de Força Relativa (RSI) antes de negociar. A abordagem reflete o Expert Advisor original, incluindo a sua filosofia de gestão de risco de reverter posições quando um novo sinal aparece.

## Lógica de negociação
1. **Filtro de tendência** – Uma média móvel simples curta (comprimento padrão 2) é usada para determinar se o mercado está em uma micro tendência de baixa ou de alta.
2. **Padrão de Candlestick** – A estratégia analisa a vela concluída mais recentemente:
   - Um **martelo** é detectado quando o corpo fica no terço superior do intervalo, a vela fica abaixo da barra anterior e o ponto médio da vela está abaixo da tendência da média móvel.
   - Um **homem enforcado** é detectado quando o corpo fica no terço superior, a vela fica mais alta do que a barra anterior e o ponto médio da vela está acima da tendência da média móvel.
3. **RSI Filtro** –
   - As negociações longas exigem que RSI esteja abaixo do limite configurável do martelo (padrão 40).
   - As negociações curtas exigem que RSI esteja acima do limite do enforcamento (padrão 60).
4. **Execução de negociação** – Em um sinal válido, a estratégia entra com `Volume + |Position|`, então as posições abertas são revertidas imediatamente quando a configuração oposta chegar.
5. **Regras de saída** – As posições são achatadas quando o RSI cruza os limites configuráveis inferior (padrão 30) ou superior (padrão 70) na direção oposta, replicando os votos de saída no código original.

## Indicadores
- **RelativeStrengthIndex** (comprimento 33 por padrão).
- **SimpleMovingAverage** (comprimento 2 por padrão) aplicado ao fechamento de velas.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Volume` | Tamanho do pedido usado para entradas. | `1` |
| `RsiPeriod` | RSI período de retrospectiva. | `33` |
| `MaPeriod` | Período de média móvel para o filtro de tendência. | `2` |
| `HammerRsiThreshold` | Valor máximo de RSI que permite uma entrada longa do martelo. | `40` |
| `HangingManRsiThreshold` | Valor mínimo de RSI que permite uma entrada curta de enforcamento. | `60` |
| `LowerExitLevel` | Limite RSI usado para fechar posições vendidas após um cruzamento ascendente. | `30` |
| `UpperExitLevel` | Limite RSI usado para fechar posições compradas após um cruzamento descendente. | `70` |
| `CandleType` | Prazo processado pela estratégia. | `1 hour` velas |

Todos os parâmetros podem ser otimizados por meio da IU de parâmetro StockSharp.

## Notas de uso
- A lógica funciona exclusivamente em velas prontas. Certifique-se de que o período de tempo e o feed de dados selecionados produzam barras completas.
- Como a lógica de reversão sempre negocia `Volume + |Position|`, as posições mudam de direção instantaneamente no sinal oposto, correspondendo ao Expert Advisor.
- Inicie o gerenciamento de risco integrado uma vez no lançamento (`StartProtection()` é chamado em `OnStarted`).

## Arquivos
- `CS/AhHmRsiStrategy.cs` – Implementação da estratégia.
- `README.md` – Documentação em inglês.
- `README_zh.md` – documentação chinesa.
- `README_ru.md` – Documentação russa.
