# Estratégia ColorMetroDuplexStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

`ColorMetroDuplexStrategy` é uma conversão em C# do expert do MetaTrader 5 **Exp_ColorMETRO_Duplex**. O robô original usa duas instâncias independentes do indicador ColorMETRO para gerenciar os módulos de trading comprado e vendido. Cada módulo opera com sua própria assinatura de candles, avalia dois envelopes RSI escalonados produzidos pelo indicador ColorMETRO e, opcionalmente, abre ou fecha posições quando os envelopes rápido e lento se cruzam.

A versão do StockSharp mantém ambos os módulos e reproduz as mesmas regras de avaliação de sinais, usando a API de alto nível para assinaturas de candles, gerenciamento de ordens e vinculação de indicadores. Um `ColorMetroIndicator` personalizado está incluído para imitar a implementação iCustom do MT5, expondo as bandas rápida e lenta do ColorMETRO junto com o valor RSI interno.

## Como funciona

1. Duas instâncias de `SignalModule` são criadas — **Long** e **Short** — cada uma com sua própria série de candles, configurações de ColorMETRO e opções de gerenciamento de operações.
2. Quando a estratégia inicia, cada módulo assina seu período configurado e vincula o `ColorMetroIndicator` por meio de `SubscribeCandles(...).BindEx(...)`.
3. Para cada candle finalizado o indicador produz:
   - A banda rápida do ColorMETRO (envelope RSI rápido).
   - A banda lenta do ColorMETRO (envelope RSI lento).
   - O valor RSI subjacente (usado apenas como referência).
4. O módulo armazena o histórico do indicador e avalia os últimos dois valores usando o deslocamento `SignalBar` configurado (correspondendo à lógica `CopyBuffer` do MT5).
5. Regras de trading:
   - **Módulo comprado**
     - *Abrir*: a banda rápida estava acima da banda lenta na barra anterior e agora está abaixo ou igual.
     - *Fechar*: a banda lenta estava acima da banda rápida na barra anterior.
   - **Módulo vendido**
     - *Abrir*: a banda rápida estava abaixo da banda lenta na barra anterior e agora está acima ou igual.
     - *Fechar*: a banda lenta estava abaixo da banda rápida na barra anterior.
6. As ordens são roteadas via `BuyMarket` / `SellMarket`. A posição líquida atual é respeitada — operações opostas encerram a exposição existente antes de abrir uma nova.

## Parâmetros

Cada módulo expõe um grupo de parâmetros dedicado. Os valores padrão espelham o expert de MT5.

### Parâmetros de mercado compartilhados

- **Long_Volume**, **Short_Volume** — tamanho da operação (lotes) usado para novas entradas.
- **Long_OpenAllowed**, **Short_OpenAllowed** — habilitar ou desabilitar a abertura de operações para o módulo.
- **Long_CloseAllowed**, **Short_CloseAllowed** — habilitar ou desabilitar saídas automáticas.
- **Long_MarginMode**, **Short_MarginMode** — modo de gerenciamento de dinheiro mantido para compatibilidade (sem efeito neste port).
- **Long_StopLoss**, **Long_TakeProfit**, **Long_Deviation**, **Short_StopLoss**, **Short_TakeProfit**, **Short_Deviation** — reservados para documentação; stops e controle de slippage não estão automatizados nesta versão.
- **Long_Magic**, **Short_Magic** — números mágicos originais do MT5 preservados como referência.

### Parâmetros do indicador

- **Long_CandleType**, **Short_CandleType** — período para cada módulo ColorMETRO.
- **Long_PeriodRSI**, **Short_PeriodRSI** — comprimento RSI usado dentro do algoritmo ColorMETRO.
- **Long_StepSizeFast**, **Short_StepSizeFast** — passo (em pontos RSI) para o envelope rápido.
- **Long_StepSizeSlow**, **Short_StepSizeSlow** — passo para o envelope lento.
- **Long_SignalBar**, **Short_SignalBar** — deslocamento de barra usado ao ler os buffers do indicador (idêntico à entrada `SignalBar` do MT5).
- **Long_AppliedPrice**, **Short_AppliedPrice** — fonte de preço para o cálculo RSI (preço de fechamento por padrão).

## Diferenças em relação ao MT5

- **Modelo de posição** — as estratégias do StockSharp trabalham com a posição líquida. O expert original armazenava posições separadas via números mágicos; o port encerra a exposição atual antes de abrir o lado oposto.
- **Gerenciamento de dinheiro** — modos de margem e configurações de desvio são preservados como parâmetros, mas não aplicados automaticamente. Use as entradas de `Volume` para controlar o tamanho.
- **Stop-loss / take-profit** — o expert MT5 colocava stops de proteção com cada ordem. A versão do StockSharp mantém as distâncias como parâmetros de referência, mas ordens de stop reais devem ser implementadas separadamente se necessário.
- **Controle de nível de tempo** — o código MT5 usava variáveis globais para garantir apenas uma operação por tempo de sinal. No StockSharp processamos cada candle finalizado uma vez e dependemos da verificação de posição líquida para prevenir entradas duplicadas.

## Notas

- O `ColorMetroIndicator` personalizado reproduz a lógica do MT5, incluindo os envelopes RSI escalonados e a memória de tendência. Ele expõe as bandas rápida/lenta e o RSI interno para gráficos ou depuração.
- Os comentários dentro do código são intencionalmente detalhados para esclarecer as decisões de portagem e auxiliar na personalização futura.
- Para habilitar a automação de stop-loss ou take-profit, estenda `SignalModule.ProcessModule` para colocar ordens de proteção usando os controles de risco do StockSharp.
