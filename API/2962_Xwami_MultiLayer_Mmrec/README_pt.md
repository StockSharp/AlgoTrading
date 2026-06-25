# Estratégia XWAMI Multicamada MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o assessor especializado original **Exp_XWAMI_NN3_MMRec.mq5** para o StockSharp. Três camadas independentes (A/B/C) executam o indicador de momentum XWAMI em diferentes períodos e combinam seus sinais dentro de uma única posição líquida. Cada camada emula o MagicNumber correspondente da versão MetaTrader, incluindo seu contador de gestão de dinheiro e níveis de proteção.

## Lógica de trading

* Para cada camada, uma série de momentum é calculada como `preço - preço[iPeriod]` usando o preço aplicado selecionado. A diferença é passada por quatro suavizadores sequenciais (métodos e comprimentos configuráveis) para obter as linhas "up" e "down" do indicador XWAMI.
* Os sinais são avaliados no deslocamento `SignalBar`. Quando a barra anterior tinha `up > down`, os vendidos dessa camada são fechados e uma entrada comprada é permitida se a barra mais recente mostrar `up <= down`. Quando a barra anterior tinha `up < down`, os comprados são fechados e uma entrada vendida é permitida quando `up >= down`.
* Antes de abrir em uma nova direção, a estratégia fecha todas as posições opostas de outras camadas para respeitar o modelo de netagem do StockSharp. Isso espelha o comportamento de fechar uma operação de magic-number oposto no código MQL.
* Níveis opcionais de stop-loss e take-profit (expressos em pontos de preço) são verificados em cada vela completada usando o máximo/mínimo da vela. Se atingidos, forçam uma saída imediata para essa camada.

## Contador de gestão de dinheiro

Cada camada mantém um histórico contínuo de suas operações mais recentes. Sempre que o número de perdas dentro do período de retrocesso configurado atinge o *LossTrigger*, o tamanho da posição muda do volume normal para o volume reduzido ("Small"). Operações bem-sucedidas ou contagens de perdas menores revertem para o tamanho normal. As direções de compra e venda mantêm seus próprios contadores, exatamente como no auxiliar MMRec original.

## Parâmetros

A estratégia expõe o conjunto completo de parâmetros do especialista MQL:

* `Layer?CandleType` – tipo de vela (período) usado pela camada (padrões: A=8h, B=4h, C=1h).
* `Layer?Period` – atraso usado para construir a série de momentum.
* `Layer?Method1..4`, `Layer?Length1..4`, `Layer?Phase1..4` – configuração de suavização para as quatro etapas XWAMI.
* `Layer?AppliedPrice` – fórmula de preço aplicado (fechamento, abertura, ponderado, Demark, etc.).
* `Layer?SignalBar` – deslocamento da barra de sinal (0 = atual, 1 = última barra fechada, padrão 1).
* `Layer?AllowBuy/SellOpen` e `Layer?AllowBuy/SellClose` – permissões para entradas e saídas.
* `Layer?NormalVolume`, `Layer?SmallVolume` – tamanho de operação em lotes (ou unidades) para modos normal e reduzido.
* `Layer?BuyTotalTrigger`, `Layer?BuyLossTrigger`, `Layer?SellTotalTrigger`, `Layer?SellLossTrigger` – contadores MMRec que controlam a mudança para o volume reduzido.
* `Layer?StopLossPoints`, `Layer?TakeProfitPoints` – níveis de proteção em pontos de preço (0 desativa o nível).

## Notas

* A versão StockSharp usa uma única posição líquida. Quando duas camadas discordam, as posições opostas são fechadas antes de entrar na nova, preservando a ordem pretendida de sinais enquanto evita hedging.
* A etapa Tillson T3 é implementada diretamente em C# para manter paridade com o algoritmo de suavização original. Outros modos de suavização são mapeados para os indicadores integrados do StockSharp (SMA, EMA, SMMA/RMA, LWMA, Jurik).
* Como as consultas de histórico de operações diferem entre plataformas, a lógica MMRec rastreia operações concluídas dentro da estratégia e reproduz os mesmos limiares sem escanear o histórico do terminal.
