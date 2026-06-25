# Estratégia Expert ZZLWA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é um port de alto nível do StockSharp do assessor especialista original **ExpertZZLWA** do MetaTrader 5. O EA oferecia três modos de operação distintos e dimensionamento de posição martingale opcional. O port mantém a estrutura do especialista original enquanto o adapta a velas e indicadores do StockSharp:

1. **Modo Original** – alterna entre operações compradas e vendidas em cada barra concluída enquanto não há posição aberta.
2. **Modo ZigZag Addition** – recria o comportamento do indicador personalizado "ZigZag LW Addition" rastreando novos swing highs e lows por meio de valores máximos/mínimos móveis.
3. **Modo Moving Average Test** – espelha a lógica de cruzamento de MA suavizada (150) vs MA simples (10) do código MQL.

Todos os modos usam offsets de stop loss e take profit de proteção configuráveis expressos em pontos de preço. A estratégia suporta dimensionamento martingale opcional onde uma nova operação é aumentada por um multiplicador após uma perda realizada, limitada por um volume máximo.

## Lógica de trading

### Modo Original

- Trabalha apenas com velas finalizadas.
- Quando não há posição aberta, a estratégia alterna entre ordens de mercado compradas e vendidas em cada nova barra.
- Stop loss e take profit são registrados através do helper integrado `StartProtection`.
- Uma vez que uma operação fecha (no stop ou no alvo), a direção oposta se torna ativa para a próxima barra.

### Modo ZigZag Addition

- Assina a série de velas selecionada e mantém indicadores `Highest` e `Lowest` móveis.
- Detecta um swing high quando o máximo da vela toca o valor mais alto atual enquanto a direção do swing anterior não era de alta. Isso recria os sinais de buffer de compra/venda do "ZigZag LW Addition".
- Detecta um swing low quando o mínimo da vela toca o valor mais baixo móvel de maneira oposta.
- Gera uma ordem de mercado na direção sinalizada imediatamente após o fechamento da vela.

### Modo Moving Average Test

- Constrói uma média móvel suavizada com comprimento 150 e uma média móvel simples com comprimento 10 (correspondendo à implementação MQL).
- Produz um sinal comprado quando a MA suavizada cruza acima da MA simples da barra anterior para a barra atual.
- Produz um sinal vendido quando a MA suavizada cruza abaixo da MA simples.
- Os sinais são processados apenas em velas fechadas.

### Tratamento do Martingale

- Após cada operação própria recebida, a estratégia rastreia a posição líquida e o preço médio de entrada.
- Quando uma posição é totalmente fechada, o lucro realizado da última operação é registrado.
- Se a operação fechou com perda e o martingale está habilitado, o volume do próximo pedido se torna `último_volume * MartingaleMultiplier` (limitado por `MaximumVolume`).
- Se a operação fechou com lucro ou o martingale está desabilitado, a estratégia volta ao volume base.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `StopLossPoints` | 600 | Distância ao stop de proteção em pontos de preço. |
| `TakeProfitPoints` | 700 | Distância ao take profit em pontos de preço. |
| `BaseVolume` | 0.01 | Tamanho de ordem padrão quando o martingale não é aplicado. |
| `UseMartingale` | false | Habilita o dimensionamento martingale quando definido como true. |
| `MartingaleMultiplier` | 2 | Multiplicador aplicado ao último volume de operação após uma perda. |
| `MaximumVolume` | 10 | Volume máximo permitido para dimensionamento martingale. |
| `Mode` | Original | Modo de operação: `Original`, `ZigZagAddition` ou `MovingAverageTest`. |
| `ZigZagTerm` | LongTerm | Preset de sensibilidade para o modo ZigZag (ShortTerm, MediumTerm, LongTerm). |
| `SlowMaPeriod` | 150 | Período da MA suavizada usada no modo MA Test. |
| `FastMaPeriod` | 10 | Período da MA simples usada no modo MA Test. |
| `CandleType` | Período de 15 minutos | Tipo de vela assinada para processamento. |

## Notas

- Os offsets de stop/take são multiplicados pelo `PriceStep` do instrumento, correspondendo ao comportamento `_Point` do MetaTrader.
- A estratégia usa exclusivamente a API de alto nível do StockSharp (`SubscribeCandles` + vinculação de indicadores).
- Os presets de sensibilidade ZigZag mapeiam para comprimentos de Highest/Lowest de 12 (Curto), 24 (Médio) e 48 (Longo). Ajuste-os se uma largura de swing diferente for necessária.
- O rastreador martingale depende de notificações de operações próprias; certifique-se de que a estratégia rode em um ambiente onde os preenchimentos são relatados corretamente.

## Diferenças de conversão vs MQL

- A versão MQL interagia com um indicador compilado `ZigZag LW Addition`. No StockSharp aproximamos os buffers usando máximos/mínimos móveis, o que entrega sinais similares sem binários externos.
- A colocação de ordens se baseia em `BuyMarket` / `SellMarket` e no helper de proteção gerenciado em vez de tickets de ordens manuais.
- O cálculo histórico de lotes no especialista original usava o histórico de deals do terminal. O port replica esse comportamento analisando operações próprias em tempo real e armazenando o último volume de operação fechado e o lucro.
- As entradas de slippage e número mágico do MQL são omitidas porque o StockSharp não as precisa para ordens de mercado neste contexto.
