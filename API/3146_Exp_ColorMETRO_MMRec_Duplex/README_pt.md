# Estratégia Exp ColorMETRO MMRec Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia porta o consultor especialista MetaTrader 5 `Exp_ColorMETRO_MMRec_Duplex` para o StockSharp. O robô original executa dois módulos independentes do indicador ColorMETRO (um longo, um curto) e aplica uma sobreposição MMRec (recálculo de gestão de dinheiro) que reduz o tamanho da posição após perdas repetidas. A versão em C# replica esse comportamento usando a API de alto nível do StockSharp para assinaturas de velas e roteamento de ordens.

## Lógica de Negociação
- Dois indicadores ColorMETRO distintos operam em tipos de velas configuráveis. O módulo longo gerencia apenas exposição comprada, enquanto o módulo curto controla a exposição vendida.
- Cada indicador produz um envelope RSI escalonado rápido e lento. A estratégia imita as chamadas `CopyBuffer` do MQL5 armazenando valores históricos e inspecionando a barra definida por `SignalBar`.
- Uma entrada longa é gerada quando a banda rápida cruza **abaixo** da banda lenta na barra inspecionada, enquanto a barra anterior ainda tinha a banda rápida acima da banda lenta. Qualquer posição vendida aberta é achatada antes de abrir o novo comprado.
- Saídas longas ocorrem quando a banda lenta na barra anterior inspecionada fica acima da banda rápida, sinalizando um regime baixista no EA original.
- Entradas e saídas curtas replicam a lógica longa (cruzamento acima para entradas, linha rápida acima da lenta na barra anterior para saídas).
- Apenas velas terminadas são processadas e a negociação é bloqueada até que o indicador reporte ambas as bandas como prontas, reproduzindo o período de aquecimento do MetaTrader.

## Gestão de Dinheiro (MMRec)
- `Strategy.Volume` define o tamanho do lote de referência. Os módulos longo e curto multiplicam-no por seus respectivos coeficientes `LongMm`/`ShortMm` ao dimensionar novas ordens.
- Após cada operação concluída, a estratégia registra se o resultado foi uma perda (baseado em preços de fechamento de velas, assim como o EA que inspeciona negociações históricas).
- Se as `TotalTrigger` operações mais recentes de um módulo contiverem pelo menos `LossTrigger` perdedoras, o módulo muda para o multiplicador reduzido (`SmallMm`). Uma vez que a contagem de perdas cai abaixo do limiar, o multiplicador padrão é restaurado automaticamente.
- Reversões de posição primeiro finalizam o resultado da operação existente (atualizando os contadores MMRec) antes de dimensionar e abrir a direção oposta.

## Notas sobre o Indicador
- `ColorMetroMmrecIndicator` é um port fiel do indicador personalizado `ColorMETRO`. Ele alimenta os mesmos envelopes rápidos/lentos impulsionados por um núcleo RSI com rastreamento de passos e memória de tendência.
- O indicador expõe o RSI interno e uma flag de prontidão para que a estratégia possa ignorar valores incompletos exatamente como a implementação MQL faz.

## Parâmetros
| Grupo | Nome | Descrição |
| --- | --- | --- |
| Comprado | `LongCandleType` | Tipo de vela usado para o módulo ColorMETRO comprado. |
| Comprado | `LongTotalTrigger` | Número de operações longas concluídas inspecionadas ao avaliar MMRec. |
| Comprado | `LongLossTrigger` | Contagem de perdas que ativa o multiplicador longo reduzido. |
| Comprado | `LongSmallMm` | Multiplicador reduzido aplicado a operações longas após uma sequência de perdas. |
| Comprado | `LongMm` | Multiplicador padrão para operações longas. |
| Comprado | `LongEnableOpen` | Habilita a abertura de posições longas. |
| Comprado | `LongEnableClose` | Habilita o fechamento de posições longas. |
| Comprado | `LongPeriodRsi` | Comprimento RSI usado dentro do indicador ColorMETRO longo. |
| Comprado | `LongStepSizeFast` | Tamanho do passo do envelope rápido para o módulo longo. |
| Comprado | `LongStepSizeSlow` | Tamanho do passo do envelope lento para o módulo longo. |
| Comprado | `LongSignalBar` | Deslocamento histórico (em barras fechadas) usado ao ler valores do indicador. |
| Comprado | `LongMagic` | Número mágico MT5 original, mantido como referência. |
| Comprado | `LongStopLossTicks` | Marcador de distância de stop-loss do EA (não aplicado). |
| Comprado | `LongTakeProfitTicks` | Marcador de distância de take-profit do EA (não aplicado). |
| Comprado | `LongDeviationTicks` | Marcador de slippage permitido do EA (não aplicado). |
| Comprado | `LongMarginMode` | Flag de modo MM mantido para compatibilidade (lógica usa multiplicadores brutos). |
| Vendido | `ShortCandleType` | Tipo de vela usado para o módulo ColorMETRO vendido. |
| Vendido | `ShortTotalTrigger` | Número de operações curtas concluídas inspecionadas ao avaliar MMRec. |
| Vendido | `ShortLossTrigger` | Contagem de perdas que ativa o multiplicador curto reduzido. |
| Vendido | `ShortSmallMm` | Multiplicador reduzido aplicado a operações curtas após uma sequência de perdas. |
| Vendido | `ShortMm` | Multiplicador padrão para operações curtas. |
| Vendido | `ShortEnableOpen` | Habilita a abertura de posições curtas. |
| Vendido | `ShortEnableClose` | Habilita o fechamento de posições curtas. |
| Vendido | `ShortPeriodRsi` | Comprimento RSI usado dentro do indicador ColorMETRO curto. |
| Vendido | `ShortStepSizeFast` | Tamanho do passo do envelope rápido para o módulo curto. |
| Vendido | `ShortStepSizeSlow` | Tamanho do passo do envelope lento para o módulo curto. |
| Vendido | `ShortSignalBar` | Deslocamento histórico (em barras fechadas) usado ao ler valores do indicador. |
| Vendido | `ShortMagic` | Número mágico MT5 original, mantido como referência. |
| Vendido | `ShortStopLossTicks` | Marcador de distância de stop-loss do EA (não aplicado). |
| Vendido | `ShortTakeProfitTicks` | Marcador de distância de take-profit do EA (não aplicado). |
| Vendido | `ShortDeviationTicks` | Marcador de slippage permitido do EA (não aplicado). |
| Vendido | `ShortMarginMode` | Flag de modo MM mantido para compatibilidade (lógica usa multiplicadores brutos). |

## Notas de Implementação
- A estratégia depende de `SubscribeCandles(...).BindEx(...)` e evita acesso direto a buffers, alinhando-se com as diretrizes de conversão.
- Stops protetores do EA são deixados apenas como parâmetros; os usuários podem anexar `StartProtection` ou módulos de risco personalizados se necessário.
- Ambos os módulos compartilham a mesma instância de instrumento, mas mantêm suas próprias assinaturas de velas e contadores MMRec, correspondendo ao layout duplex do MetaTrader.
- Todos os comentários no código são fornecidos em inglês e a lógica evita usar chamadas de API proibidas como `GetTrades`.

## Aviso
Este port reproduz a estrutura lógica do EA original, mas a qualidade de execução depende do broker conectado, feed de dados e configuração do StockSharp. Sempre valide o comportamento em dados históricos e de demonstração antes de negociar com capital real.
